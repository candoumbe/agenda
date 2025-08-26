using System.Text.Json;
using System.Text.Json.Serialization;
using Agenda.API;
using Agenda.API.Resources.Appointments.v1.Update;
using Agenda.API.TypeMappers;
using Agenda.DataStores;
using Agenda.Ids;
using Candoumbe.Types.Numerics;
using DataFilters.Converters;
using FastEndpoints;
using FastEndpoints.Swagger;
using Fluxera.StronglyTypedId.SystemTextJson;
using Json.More;
using Json.Patch;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.JsonPatch.Converters;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Serilog;
using static Microsoft.AspNetCore.Http.StatusCodes;

Action<JsonSerializerOptions> optionsSerializerSettings = s =>
                                                          {
                                                              //s.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                                                              s.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                                                              s.AllowTrailingCommas = true;
                                                              s.UseStronglyTypedId();
                                                              s.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
                                                              s.Converters.Add(new MultiFilterConverter());
                                                              s.Converters.Add(new FilterConverter());
                                                              s.Converters.Add(new PatchJsonConverter());
                                                              s.Converters.Add(new JsonStringEnumConverter<OperationType>());
                                                              s.Converters.Add(new EnumStringConverter<OperationType>());
                                                          };

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


builder.AddServiceDefaults();
builder.Services.AddCustomizedDependencyInjection();
builder.AddNpgsqlDbContext<AgendaDataStore>("postgres",
                                            configureDbContextOptions: optionsBuilder =>
                                                                       {
                                                                           optionsBuilder.UseNpgsql(o => o.UseNodaTime()
                                                                                                        .MigrationsAssembly("Agenda.DataStores.Postgres"));
                                                                       });
builder.Services.AddDataStores(builder.Configuration);
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
                                                        docSettings.SchemaSettings.TypeMappers.Add(new StronglyTypedIdMapper<AppointmentId, Guid>());
                                                        docSettings.SchemaSettings.TypeMappers.Add(new StronglyTypedIdMapper<AttendeeId, Guid>());
                                                        docSettings.SchemaSettings.TypeMappers.Add(new NumberTypeMapper<PositiveInteger, int>());
                                                        docSettings.SchemaSettings.TypeMappers.Add(new NumberTypeMapper<NonNegativeInteger, int>());
                                                    };
                         options.SerializerSettings = optionsSerializerSettings;
                     })
    .AddFastEndpoints(options =>
                      {
                          options.IncludeAbstractValidators = true;
                      });

WebApplication app = builder.Build();


// app.UseSerilogRequestLogging(opts => opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) => diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier));
app.UseFastEndpoints(config =>
                     {
                         config.Binding.ValueParserFor<AppointmentId>(values => new ParseResult(AppointmentId.TryParse(values.ToString(), out AppointmentId id), id));
                         // config.Binding.ValueParserFor<NonNegativeInteger>(values => new ParseResult(int.TryParse(values.ToString(), out int value)
                         //                                                                             && NonNegativeInteger.MinValue <= value && value <= NonNegativeInteger.MaxValue, NonNegativeInteger.From(value)));
                         // config.Binding.ValueParserFor<PositiveInteger>(values => new ParseResult(int.TryParse(values.ToString(), out int value)
                         //                                                                              && PositiveInteger.MinValue <= value
                         //                                                                              && value <= PositiveInteger.MaxValue,
                         //                                                                          PositiveInteger.From(value)));

                         config.Errors.UseProblemDetails(detailsConfig =>
                                                         {
                                                             detailsConfig.AllowDuplicateErrors = true;
                                                             detailsConfig.IndicateErrorCode = true;
                                                             detailsConfig.TypeTransformer = problemDetails => problemDetails.Status switch
                                                             {
                                                                 Status404NotFound => "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.4",
                                                                 Status409Conflict => "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.8",
                                                                 Status429TooManyRequests => "https://www.rfc-editor.org/rfc/rfc6585#section-4",
                                                                 _ => "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1"
                                                             };
                                                         });

                         optionsSerializerSettings.Invoke(config.Serializer.Options);
                     })
    .UseSwaggerGen();

await app.RunAsync().ConfigureAwait(false);

return;


/// <summary>
/// Application entry point
/// </summary>
public partial class Program;