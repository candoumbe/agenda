using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agenda.API;
using Agenda.API.Features;
using Agenda.API.TypeMappers;
using Agenda.DataStores;
using Agenda.DataStores.Postgres;
using Agenda.Ids;
using Asp.Versioning;
using Candoumbe.Types.Numerics;
using FastEndpoints;
using FastEndpoints.AspVersioning;
using FastEndpoints.OpenApi;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Scalar.AspNetCore;
using Serilog;
using SystemTextJsonPatch.Operations;
using static Microsoft.AspNetCore.Http.StatusCodes;

Action<JsonSerializerOptions> optionsSerializerSettings = s =>
                                                          {
                                                              //s.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                                                              s.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                                                              s.AllowTrailingCommas = true;
                                                              s.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
                                                              s.Converters.Add(new JsonStringEnumConverter<OperationType>());
                                                          };

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AgendaDataStore>("postgres",
    configureSettings: settings => settings.ConnectionString = builder.Configuration.GetConnectionString("postgres")!.WithGssDisabled(),
    configureDbContextOptions: optionsBuilder =>
    {
        optionsBuilder.UseNpgsql(o => o.UseNodaTime()
            .MigrationsAssembly("Agenda.DataStores.Postgres")
            );
    });
builder.Services.AddCustomizedDependencyInjection(builder.Configuration);


builder.Services.AddDataStores();
builder.Services.AddCustomBrighter(builder.Configuration, builder.Environment);
builder.Services.AddCustomAuthentication(builder.Configuration, builder.Environment);

// The parameterless overload binds the uninitialised static Log.Logger and silences every sink.
builder.Services.AddSerilog((serviceProvider, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(builder.Configuration)
                                                                                        .ReadFrom.Services(serviceProvider));
builder.Services.Configure<JsonOptions>(c => optionsSerializerSettings.Invoke(c.SerializerOptions));
builder.Services
    .OpenApiDocument(options =>
                     {
                         options.MaxEndpointVersion = 1;
                         options.ShortSchemaNames = true;
                         options.ShowDeprecatedOps = true;
                         options.DocumentName = "v1";
                         options.Title = "Agenda API";
                         options.Version = "v1";
                         options.ConfigureOpenApi = docSettings =>
                         {
                             docSettings.AddSchemaTransformer<NumberTypeSchemaTransformer<PositiveInteger, int>>();
                             docSettings.AddSchemaTransformer<NumberTypeSchemaTransformer<NonNegativeInteger, int>>();
                         };
                     });

builder.Services.AddFastEndpoints(options => options.IncludeAbstractValidators = false)
                .AddVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ReportApiVersions = true;
                    options.ApiVersionReader = new HeaderApiVersionReader("api-version");
                    options.UnsupportedApiVersionStatusCode = Status400BadRequest;
                });


WebApplication app = builder.Build();
AddLinkHeaderResponseInterceptor addLinkHeaderResponseInterceptor = new(app.Services.GetRequiredService<ILogger<AddLinkHeaderResponseInterceptor>>());

// app.UseSerilogRequestLogging(opts => opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) => diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier));

// OpenAPI documentation must remain reachable under the JWT FallbackPolicy. The branch isolates UseOpenApi
// from the parent auth pipeline so the document is served before authorization is evaluated.
app.MapOpenApi().AllowAnonymous();

app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(config =>
                     {
                         config.Security.RoleClaimType = ClaimTypes.Role;
                         config.Binding.ValueParserFor<AppointmentId>(values => new ParseResult(AppointmentId.TryParse(values.ToString(), CultureInfo.InvariantCulture, out AppointmentId id), id));
                         config.Binding.ValueParserFor<AttendeeId>(values => new ParseResult(AttendeeId.TryParse(values.ToString(), CultureInfo.InvariantCulture, out AttendeeId id), id));
                         config.Binding.ValueParserFor<NonNegativeInteger>(values => new ParseResult(int.TryParse(values.ToString(), out int value)
                                                                                                     && NonNegativeInteger.MinValue <= value && value <= NonNegativeInteger.MaxValue, NonNegativeInteger.From(value)));
                         config.Binding.ValueParserFor<PositiveInteger>(values => new ParseResult(int.TryParse(values.ToString(), out int value)
                                                                                                      && PositiveInteger.MinValue <= value
                                                                                                      && value <= PositiveInteger.MaxValue,
                                                                                                  PositiveInteger.From(value)));

                         config.Endpoints.GlobalResponseModifierAsync = (httpContext, response) =>
                         {
                            if (response is null)
                            {
                                return Task.CompletedTask;
                            }

                            return addLinkHeaderResponseInterceptor.InterceptResponseAsync(response,
                                                                                           httpContext.Response.StatusCode,
                                                                                           httpContext,
                                                                                           [],
                                                                                           httpContext.RequestAborted);
                         };

                         config.Errors.UseProblemDetails(detailsConfig =>
                                                         {
                                                             detailsConfig.AllowDuplicateErrors = true;
                                                             detailsConfig.IndicateErrorCode = true;
                                                             detailsConfig.TypeTransformer = problemDetails => problemDetails.Status switch
                                                             {
                                                                 Status200OK => "https://www.rfc-editor.org/rfc/rfc7231#section-6.3.1",
                                                                 Status404NotFound => "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.4",
                                                                 Status409Conflict => "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.8",
                                                                 Status429TooManyRequests => "https://www.rfc-editor.org/rfc/rfc6585#section-4",
                                                                 _ => "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1"
                                                             };
                                                         });

                         optionsSerializerSettings.Invoke(config.Serializer.Options);
                     });

// API documentation must remain reachable in every environment, including production.
app.MapScalarApiReference(options =>
{
    options.AddDocument("v1");
}).AllowAnonymous();

// Scalar emits relative asset URLs, so browsing "/scalar/v1/" resolves them one segment too deep.
// Redirecting back to the canonical asset path keeps the reference page usable with a trailing slash.
app.MapGet("/scalar/{documentName}/{**asset}", (string asset) => Results.LocalRedirect($"/scalar/{asset}"))
   .AllowAnonymous()
   .ExcludeFromDescription();

app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);

return;


/// <summary>
/// Application entry point
/// </summary>
public partial class Program
{

}