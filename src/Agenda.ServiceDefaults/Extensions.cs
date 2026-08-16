#nullable enable
using System.Data.Common;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
                                                     {
                                                         // Turn on resilience by default
                                                         http.AddStandardResilienceHandler();

                                                         // Turn on service discovery by default
                                                         http.AddServiceDiscovery();
                                                     });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry.
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="TBuilder"></typeparam>
    /// <returns></returns>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
                                         {
                                             logging.IncludeFormattedMessage = true;
                                             logging.IncludeScopes = true;
                                         });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
                         {
                             metrics.AddAspNetCoreInstrumentation()
                                 .AddHttpClientInstrumentation()
                                 .AddRuntimeInstrumentation();
                         })
            .WithTracing(tracing =>
                         {
                             tracing.AddSource(builder.Environment.ApplicationName)
                                 .AddAspNetCoreInstrumentation(tracingBuiler =>
                                                                   // Exclude health check requests from tracing
                                                                   tracingBuiler.Filter = context =>
                                                                                        !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                                                                                        && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                                                              )
                                 // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                                 //.AddGrpcClientInstrumentation()
                                 .AddHttpClientInstrumentation();
                         });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        bool useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    /// <summary>
    /// Configures default health checks.
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="TBuilder">Type of the extended builder.</typeparam>
    /// <returns></returns>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        bool isRunningIntegrationTests = builder.Configuration.GetValue("RunningIntegrationTests", false);

        IHealthChecksBuilder healthChecksBuilder = builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        if (!isRunningIntegrationTests)
        {
            TryAddPostgresHealthCheck(builder, healthChecksBuilder);
            TryAddRabbitMqHealthCheck(builder, healthChecksBuilder);
        }

        return builder;
    }

    private static void TryAddPostgresHealthCheck(IHostApplicationBuilder builder, IHealthChecksBuilder healthChecksBuilder)
    {
        string? postgresConnectionString = builder.Configuration.GetConnectionString("postgres");

        if (string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            return;
        }

        if (TryGetPostgresEndpoint(postgresConnectionString, out string host, out int port))
        {
            healthChecksBuilder.AddCheck("postgres", new TcpDependencyHealthCheck(host, port), HealthStatus.Healthy, ["ready"]);
        }
    }

    private static void TryAddRabbitMqHealthCheck(IHostApplicationBuilder builder, IHealthChecksBuilder healthChecksBuilder)
    {
        string? rabbitMqConnectionString = builder.Configuration.GetConnectionString("messaging");

        if (string.IsNullOrWhiteSpace(rabbitMqConnectionString))
        {
            return;
        }

        if (TryGetRabbitMqEndpoint(rabbitMqConnectionString, out string host, out int port))
        {
            healthChecksBuilder.AddCheck("messaging", new TcpDependencyHealthCheck(host, port), HealthStatus.Healthy, ["ready"]);
        }
    }

    private static bool TryGetPostgresEndpoint(string connectionString, out string host, out int port)
    {
        host = string.Empty;
        port = 5432;

        DbConnectionStringBuilder connectionStringBuilder = new() { ConnectionString = connectionString };

        object? hostValue;
        bool hasHost = connectionStringBuilder.TryGetValue("Host", out hostValue)
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

    private static bool TryGetRabbitMqEndpoint(string connectionString, out string host, out int port)
    {
        host = string.Empty;
        port = 5672;

        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        host = uri.Host;
        port = uri.Port > 0 ? uri.Port : 5672;

        return true;
    }

    private sealed class TcpDependencyHealthCheck : IHealthCheck
    {
        private readonly string _host;
        private readonly int _port;

        public TcpDependencyHealthCheck(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            using TcpClient tcpClient = new();

            try
            {
                using CancellationTokenSource timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(2));

                await tcpClient.ConnectAsync(_host, _port, timeoutCancellationTokenSource.Token);
                return HealthCheckResult.Healthy();
            }
            catch (Exception exception)
            {
                return HealthCheckResult.Unhealthy(exception.Message, exception);
            }
        }
    }

    /// <summary>
    /// Maps default endpoints to the application.
    /// </summary>
    /// <param name="app">The app which health checks are enabled for.</param>
    /// <returns></returns>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // All health checks must pass for app to be considered ready to accept traffic after starting.
        // AllowAnonymous keeps health probes reachable when a global authentication FallbackPolicy is configured.
        app.MapHealthChecks(HealthEndpointPath).AllowAnonymous();

        // Liveness must represent process availability only, independent of external dependencies.
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions { Predicate = r => r.Name == "self" })
            .AllowAnonymous();

        return app;
    }
}