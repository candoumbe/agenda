using Agenda.API;
using Agenda.DataStores;
using Agenda.Ids;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using Serilog;

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
builder.Services
    .SwaggerDocument(options =>
                     {
                         options.MaxEndpointVersion = 1;
                         options.ShortSchemaNames = true;
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
                     })
    .UseSwaggerGen();

await app.RunAsync().ConfigureAwait(false);

return;


/// <summary>
/// Application entry point
/// </summary>
public partial class Program;