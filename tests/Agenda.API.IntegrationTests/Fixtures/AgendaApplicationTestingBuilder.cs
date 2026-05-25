using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
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
    private const int s_requiredConsecutiveSuccessfulProbes = 3;
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

        ApiClient = _app.CreateHttpClient(ApiResourceName, endpointName: "http");
        await WaitUntilApiIsReachableAsync(cancellationToken);

        return _app;
    }

    private async Task WaitUntilApiIsReachableAsync(CancellationToken cancellationToken)
    {
        Exception lastException = null;
        int consecutiveSuccessCount = 0;

        using CancellationTokenSource timeoutCancellationTokenSource = new(s_startStopTimeout);
        using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token);

        while (!linkedCancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                // Probe a datastore-backed endpoint so tests only start once
                // API + database + migrations are effectively usable.
                using HttpRequestMessage request = new(HttpMethod.Get, "/appointments?page=1&pageSize=1");
                using CancellationTokenSource requestCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(linkedCancellationTokenSource.Token);
                requestCancellationTokenSource.CancelAfter(s_requestProbeTimeout);

                using HttpResponseMessage response = await ApiClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestCancellationTokenSource.Token);

                if (response.IsSuccessStatusCode)
                {
                    consecutiveSuccessCount++;
                    if (consecutiveSuccessCount >= s_requiredConsecutiveSuccessfulProbes)
                    {
                        return;
                    }
                }
                else
                {
                    consecutiveSuccessCount = 0;
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                lastException = exception;
                consecutiveSuccessCount = 0;
            }

            await Task.Delay(s_readinessProbeDelay, linkedCancellationTokenSource.Token);
        }

        throw new TimeoutException("The API endpoint did not become reachable before the startup timeout elapsed.", lastException);
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