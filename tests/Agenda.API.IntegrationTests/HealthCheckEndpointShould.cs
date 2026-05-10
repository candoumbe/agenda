using System;
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
public class HealthCheckEndpointShould(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    private HttpClient _client;
    private AgendaApplicationTestingBuilder _appHost;
    private static readonly TimeSpan s_buildStopTimeout = TimeSpan.FromSeconds(60);
    
    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(outputHelper, TestContext.Current.CancellationToken);
        await _appHost.StartAsync(TestContext.Current.CancellationToken);
        _client = _appHost.ApiClient;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // ReSharper disable DisposeOnUsingVariable
        await _appHost.DisposeAsync();
        // ReSharper restore DisposeOnUsingVariable
    }

    [RetryFact(maxRetries: 3, delayBetweenRetriesMs: 2000, SkipExceptions = [typeof(DistributedApplicationException)])]
    public async Task Return_healthy_status_from_health_endpoint()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        using HttpResponseMessage response = await _client.GetAsync("/health", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [RetryFact(maxRetries: 3, delayBetweenRetriesMs: 2000, SkipExceptions = [typeof(DistributedApplicationException)])]
    public async Task Return_healthy_status_from_alive_endpoint()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        using HttpResponseMessage response = await _client.GetAsync("/alive", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
