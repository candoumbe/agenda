using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Projects;
using System.Data.Common;
using System.Net.Sockets;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

bool isRunningIntegrationTests = builder.Configuration.GetValue(RunningIntegrationTestsConfigName, false);
bool shouldTrackResourceHealth = !isRunningIntegrationTests;

if (shouldTrackResourceHealth)
{
    IHealthChecksBuilder healthChecksBuilder = builder.Services.AddHealthChecks();
    healthChecksBuilder.AddCheck(
        "postgres",
        new ConnectionStringTcpHealthCheck(builder.Configuration, "postgres", ConnectionType.Postgres),
        HealthStatus.Unhealthy,
        ["ready"]);
    healthChecksBuilder.AddCheck(
        "messaging",
        new ConnectionStringTcpHealthCheck(builder.Configuration, "messaging", ConnectionType.Uri),
        HealthStatus.Unhealthy,
        ["ready"]);
}

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres:17-alpine");

if (shouldTrackResourceHealth)
{
    postgres = postgres.WithHealthCheck("postgres");
}

if (builder.ExecutionContext.IsRunMode && !isRunningIntegrationTests)
{
    postgres = postgres
            .WithPgAdmin(containerName: "pg-admin")
            .WithPgWeb(containerName: "pg-web");
}

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

if (shouldTrackResourceHealth)
{
    messaging = messaging.WithHealthCheck("messaging");
}

IResourceBuilder<ProjectResource> migrationService = builder.AddProject<Agenda_Migrator>("migrations")
    .WithReference(postgres)
    .WaitFor(postgres);
    

IResourceBuilder<ProjectResource> api = builder.AddProject<Agenda_API>("api")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(postgres)
    .WithReference(messaging).WaitFor(messaging)
    .WaitForCompletion(migrationService);

if (builder.ExecutionContext.IsPublishMode)
{
    api = api.PublishAsDockerFile();
}

if (!isRunningIntegrationTests)
{
    api = api.WithDeveloperCertificateTrust(trust: true);
}

string runScriptName = builder.ExecutionContext.IsRunMode ? "start:dev" : "start";

if (!isRunningIntegrationTests)
{
     builder.AddJavaScriptApp("frontend", "../Agenda.Frontend", runScriptName)
              .WithDeveloperCertificateTrust(trust: true)
              .WithReference(api)
              .WaitFor(api)
                // Demande à Aspire d’allouer un port et de le passer à l’app via la variable d’env PORT
              .WithHttpEndpoint(env: "PORT")
              .WithExternalHttpEndpoints()
          .PublishAsDockerFile();
}

builder.Build().Run();


public partial class Program
{
    public const string RunningIntegrationTestsConfigName = "RunningIntegrationTests";
}

internal enum ConnectionType
{
    Postgres,
    Uri
}

internal sealed class ConnectionStringTcpHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionName;
    private readonly ConnectionType _connectionType;

    public ConnectionStringTcpHealthCheck(IConfiguration configuration, string connectionName, ConnectionType connectionType)
    {
        _configuration = configuration;
        _connectionName = connectionName;
        _connectionType = connectionType;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        string connectionString = _configuration.GetConnectionString(_connectionName) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy($"Connection string '{_connectionName}' is missing.");
        }

        bool canExtractEndpoint = TryGetEndpoint(connectionString, _connectionType, out string host, out int port);

        if (!canExtractEndpoint)
        {
            return HealthCheckResult.Unhealthy($"Connection string '{_connectionName}' does not contain a valid endpoint.");
        }

        using TcpClient tcpClient = new();

        try
        {
            using CancellationTokenSource timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(2));

            await tcpClient.ConnectAsync(host, port, timeoutCancellationTokenSource.Token);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(exception.Message, exception);
        }
    }

    private static bool TryGetEndpoint(string connectionString, ConnectionType connectionType, out string host, out int port)
    {
        host = string.Empty;
        port = connectionType == ConnectionType.Postgres ? 5432 : 5672;

        if (connectionType == ConnectionType.Uri)
        {
            if (!Uri.TryCreate(connectionString, UriKind.Absolute, out Uri uri) || string.IsNullOrWhiteSpace(uri.Host))
            {
                return false;
            }

            host = uri.Host;
            port = uri.Port > 0 ? uri.Port : 5672;
            return true;
        }

        DbConnectionStringBuilder connectionStringBuilder = new() { ConnectionString = connectionString };

        bool hasHost = connectionStringBuilder.TryGetValue("Host", out object? hostValue)
            || connectionStringBuilder.TryGetValue("Server", out hostValue);

        if (!hasHost)
        {
            return false;
        }

        host = hostValue?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        if (!connectionStringBuilder.TryGetValue("Port", out object? portValue))
        {
            return true;
        }

        return int.TryParse(portValue?.ToString(), out port);
    }
}