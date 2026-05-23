using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Agenda.API.IntegrationTests.Fixtures;

public class AgendaApplicationTestingBuilder : IAsyncLifetime
{
    private readonly IDistributedApplicationTestingBuilder _sutBuilder;
    private readonly string _previousRunningIntegrationTestsValue;
    private DistributedApplication _app;
    /// <summary>
    /// HTTP client for the API.
    /// </summary>
    public HttpClient ApiClient { get; private set; }
    public const string ApiResourceName = "api";

    /// <summary>
    /// Time to wait after which the application under test will be considered as "not started".
    /// </summary>
    private static readonly TimeSpan s_startStopTimeout = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan s_readinessProbeDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan s_requestProbeTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan s_dependencyProbeTimeout = TimeSpan.FromSeconds(5);
    /// <summary>
    /// Time to wait after which building the infrastructure will be considered as failed.
    /// </summary>
    private static readonly TimeSpan s_buildStopTimeout = TimeSpan.FromSeconds(60);


    /// <summary>
    /// Creates a new instance of the <see cref="AgendaApplicationTestingBuilder"/> class.
    /// </summary>
    /// <param name="builder">The builder that will be used to create the infrastructure of the application under test.</param>
    /// <param name="previousRunningIntegrationTestsValue">The previous value of the <c>RunningIntegrationTests</c> environment variable to restore on dispose.</param>
    public AgendaApplicationTestingBuilder(IDistributedApplicationTestingBuilder builder, string previousRunningIntegrationTestsValue = null)
    {
        _sutBuilder = builder;
        _previousRunningIntegrationTestsValue = previousRunningIntegrationTestsValue;
    }

    /// <summary>
    /// Builds the infrastructure and starts the application under test.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>The application under test</returns>
    /// <remarks>
    /// The application under test is started after the infrastructure is built.
    /// This method will wait for the application to reach the "running" state (i.e. all resources are running or have exited with a success code).
    /// </remarks>
    public async Task<DistributedApplication> StartAsync(CancellationToken cancellationToken)
    {
        _app  = await _sutBuilder.BuildAsync(cancellationToken).WaitAsync(s_buildStopTimeout, cancellationToken);

        await _app.StartAsync(cancellationToken).WaitAsync(s_startStopTimeout, cancellationToken);
        await _app.WaitForResourcesAsync(cancellationToken: cancellationToken).WaitAsync(s_startStopTimeout, cancellationToken);
        await WaitUntilDependenciesAreReadyAsync(cancellationToken);

        ApiClient = _app.CreateHttpClient(ApiResourceName, endpointName: "http");
        await WaitUntilApiIsReadyAsync(cancellationToken);

        return _app;
    }

    private async Task WaitUntilDependenciesAreReadyAsync(CancellationToken cancellationToken)
    {
        IConfiguration configuration = _app.Services.GetRequiredService<IConfiguration>();
        string postgresConnectionString = configuration.GetConnectionString("postgres");
        string messagingConnectionString = configuration.GetConnectionString("messaging");

        if (string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            throw new InvalidOperationException("The 'postgres' connection string is missing in integration test runtime configuration.");
        }

        if (string.IsNullOrWhiteSpace(messagingConnectionString))
        {
            throw new InvalidOperationException("The 'messaging' connection string is missing in integration test runtime configuration.");
        }

        await WaitUntilPostgresIsReadyAsync(postgresConnectionString, cancellationToken);
        await WaitUntilTcpEndpointIsReadyAsync(messagingConnectionString, "messaging", cancellationToken);
    }

    private async Task WaitUntilPostgresIsReadyAsync(string postgresConnectionString, CancellationToken cancellationToken)
    {
        Exception lastException = null;

        using CancellationTokenSource timeoutCancellationTokenSource = new(s_startStopTimeout);
        using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token);

        while (!linkedCancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                await using NpgsqlConnection connection = new(postgresConnectionString);
                using CancellationTokenSource requestCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(linkedCancellationTokenSource.Token);
                requestCancellationTokenSource.CancelAfter(s_dependencyProbeTimeout);

                await connection.OpenAsync(requestCancellationTokenSource.Token);
                await using NpgsqlCommand command = new("SELECT 1", connection);
                await command.ExecuteScalarAsync(requestCancellationTokenSource.Token);

                return;
            }
            catch (Exception exception) when (exception is NpgsqlException or SocketException or TimeoutException or TaskCanceledException)
            {
                lastException = exception;
            }

            await Task.Delay(s_readinessProbeDelay, linkedCancellationTokenSource.Token);
        }

        throw new TimeoutException("Postgres did not become queryable before the startup timeout elapsed.", lastException);
    }

    private async Task WaitUntilTcpEndpointIsReadyAsync(string connectionString, string resourceName, CancellationToken cancellationToken)
    {
        Exception lastException = null;

        using CancellationTokenSource timeoutCancellationTokenSource = new(s_startStopTimeout);
        using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token);

        Uri endpointUri = new(connectionString);
        int port = endpointUri.IsDefaultPort ? 5672 : endpointUri.Port;

        while (!linkedCancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                using CancellationTokenSource requestCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(linkedCancellationTokenSource.Token);
                requestCancellationTokenSource.CancelAfter(s_dependencyProbeTimeout);

                using TcpClient tcpClient = new();
                await tcpClient.ConnectAsync(endpointUri.Host, port, requestCancellationTokenSource.Token);

                return;
            }
            catch (Exception exception) when (exception is SocketException or TimeoutException or TaskCanceledException)
            {
                lastException = exception;
            }

            await Task.Delay(s_readinessProbeDelay, linkedCancellationTokenSource.Token);
        }

        throw new TimeoutException($"{resourceName} did not start accepting TCP connections before the startup timeout elapsed.", lastException);
    }

    private async Task WaitUntilApiIsReadyAsync(CancellationToken cancellationToken)
    {
        Exception lastException = null;

        using CancellationTokenSource timeoutCancellationTokenSource = new(s_startStopTimeout);
        using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token);

        while (!linkedCancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Get, "/health");
                using CancellationTokenSource requestCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(linkedCancellationTokenSource.Token);
                requestCancellationTokenSource.CancelAfter(s_requestProbeTimeout);

                using HttpResponseMessage response = await ApiClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestCancellationTokenSource.Token);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                lastException = exception;
            }

            await Task.Delay(s_readinessProbeDelay, linkedCancellationTokenSource.Token);
        }

        throw new TimeoutException("The API health endpoint did not become ready before the startup timeout elapsed.", lastException);
    }


    /// <inheritdoc />
    public async ValueTask InitializeAsync() => await StartAsync(TestContext.Current.CancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Approche en deux phases : arrêt gracieux puis forcé
        bool stopped = await TryGracefulStopAsync();

        if (!stopped && _app is not null)
        {
            Console.WriteLine("Arrêt gracieux échoué, nettoyage forcé...");
            await _app.DisposeAsync();
        }

        await _sutBuilder.DisposeAsync();

        // Restore the RunningIntegrationTests environment variable to its previous value
        // to avoid leaking global state into other tests in the same process.
        Environment.SetEnvironmentVariable("RunningIntegrationTests", _previousRunningIntegrationTestsValue);
    }

    private async Task<bool> TryGracefulStopAsync()
    {
        if (_app == null)
        {
            return true;
        }

        try
        {
            // Timeout plus court pour l'arrêt gracieux
            using CancellationTokenSource cts = new (s_startStopTimeout);
            await _app.StopAsync(cts.Token);
            return true;
        }
        catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
        {
            Console.WriteLine($"Timeout lors de l'arrêt gracieux: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'arrêt gracieux: {ex.Message}");
            return false;
        }
    }
}