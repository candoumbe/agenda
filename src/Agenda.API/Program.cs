using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agenda.API;
using Agenda.API.TypeMappers;
using Agenda.DataStores;
using Agenda.Ids;
using Asp.Versioning;
using Candoumbe.Types.Numerics;
using FastEndpoints;
using FastEndpoints.AspVersioning;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
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
    configureDbContextOptions: optionsBuilder =>
    {
        optionsBuilder.UseNpgsql(o => o.UseNodaTime()
            .MigrationsAssembly("Agenda.DataStores.Postgres"));
    });
builder.Services.AddCustomizedDependencyInjection();


builder.Services.AddDataStores();
builder.Services.AddCustomBrighter(builder.Configuration, builder.Environment);
builder.Services.AddSerilog();
builder.Services.Configure<JsonOptions>(c => optionsSerializerSettings.Invoke(c.SerializerOptions));
builder.Services
    .SwaggerDocument(options =>
                     {
                         options.MaxEndpointVersion = 1;
                         options.ShortSchemaNames = true;
                         options.ShowDeprecatedOps = true;
                         options.DocumentSettings = docSettings =>
                                                    {
                                                        docSettings.SchemaSettings.AllowReferencesWithProperties = true;

                                                        docSettings.SchemaSettings.TypeMappers.Add(new NumberTypeMapper<PositiveInteger, int>());
                                                        docSettings.SchemaSettings.TypeMappers.Add(new NumberTypeMapper<NonNegativeInteger, int>());
                                                    };
                         options.SerializerSettings = optionsSerializerSettings;
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

app.MapDefaultEndpoints();

// app.UseSerilogRequestLogging(opts => opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) => diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier));
app.UseFastEndpoints(config =>
                     {
                         config.Binding.ValueParserFor<AppointmentId>(values => new ParseResult(AppointmentId.TryParse(values.ToString(), CultureInfo.InvariantCulture, out AppointmentId id), id));
                         config.Binding.ValueParserFor<AttendeeId>(values => new ParseResult(AttendeeId.TryParse(values.ToString(), CultureInfo.InvariantCulture, out AttendeeId id), id));
                         config.Binding.ValueParserFor<NonNegativeInteger>(values => new ParseResult(int.TryParse(values.ToString(), out int value)
                                                                                                     && NonNegativeInteger.MinValue <= value && value <= NonNegativeInteger.MaxValue, NonNegativeInteger.From(value)));
                         config.Binding.ValueParserFor<PositiveInteger>(values => new ParseResult(int.TryParse(values.ToString(), out int value)
                                                                                                      && PositiveInteger.MinValue <= value
                                                                                                      && value <= PositiveInteger.MaxValue,
                                                                                                  PositiveInteger.From(value)));

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
                     })
    .UseSwaggerGen();

app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);

return;


/// <summary>
/// Application entry point
/// </summary>
public partial class Program
{

}