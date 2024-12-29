using Agenda.API;
using Agenda.DataStores;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddAsyncInitializer<DataStoreMigrateInitializerAsync<AgendaDataStore>>();
builder.Services.AddCustomOptions(builder.Configuration);
builder.Services.AddDataStores(builder.Configuration);
builder.Services.AddCustomizedDependencyInjection();
builder.Services.AddCustomizedMvc(builder.Configuration);
builder.Services.AddCustomizedSwagger(builder.Configuration);

builder.Host.UseSerilog((hosting, loggerConfig) => loggerConfig.MinimumLevel.Verbose()
            .Enrich.WithProperty("ApplicationContext", hosting.HostingEnvironment.ApplicationName)
            .Enrich.FromLogContext()
            .Enrich.WithCorrelationIdHeader()
            .ReadFrom.Configuration(hosting.Configuration));

WebApplication app = builder.Build();

app.UseSerilogRequestLogging();
app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

using IServiceScope scope = app.Services.CreateScope();
IServiceProvider services = scope.ServiceProvider;
ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
IHostEnvironment env = services.GetRequiredService<IHostEnvironment>();

try
{
    logger?.LogInformation("Starting {ApplicationContext}", env.ApplicationName);
    await app.InitAsync().ConfigureAwait(false);
    await app.RunAsync().ConfigureAwait(false);

    logger?.LogInformation("{ApplicationContext} started", env.ApplicationName);
}
catch (Exception ex)
{
    logger?.LogError(ex, "An error occurred on startup.");
}

/// <summary>
/// Application entry point
/// </summary>
public partial class Program
{
}