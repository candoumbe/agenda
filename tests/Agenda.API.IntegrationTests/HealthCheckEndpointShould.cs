using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using Aspire.Hosting;
using AwesomeAssertions;
using xRetry.v3;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests;

[IntegrationTest]
public class HealthCheckEndpointShould(ITestOutputHelper outputHelper)
{
    private static readonly TimeSpan s_startStopTimeout = TimeSpan.FromSeconds(120);

    private HttpClient _client;
    private AgendaApplicationTestingBuilder _appHost;

    private async Task InitializeAsync()
    {
        _appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(outputHelper, TestContext.Current.CancellationToken);
        await _appHost.StartAsync(TestContext.Current.CancellationToken).WaitAsync(s_startStopTimeout, TestContext.Current.CancellationToken);
        _client = _appHost.ApiClient;
    }

    private async ValueTask CleanupAsync()
    {
        if (_appHost is not null)
        {
            await _appHost.DisposeAsync();
        }
    }

    [RetryFact(maxRetries: 3, delayBetweenRetriesMs: 2000, SkipExceptions = [typeof(DistributedApplicationException)])]
    public async Task Return_healthy_status_from_health_endpoint()
    {
        // Arrange
        await InitializeAsync();

        try
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            // Act
            using HttpResponseMessage response = await _client.GetAsync("/health", cancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await CleanupAsync();
        }
    }

    [RetryFact(maxRetries: 3, delayBetweenRetriesMs: 2000, SkipExceptions = [typeof(DistributedApplicationException)])]
    public async Task Return_healthy_status_from_alive_endpoint()
    {
        // Arrange
        await InitializeAsync();

        try
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            // Act
            using HttpResponseMessage response = await _client.GetAsync("/alive", cancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await CleanupAsync();
        }
    }
}
