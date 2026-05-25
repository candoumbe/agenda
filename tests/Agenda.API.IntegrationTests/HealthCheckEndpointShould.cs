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
[Collection("AgendaApplication")]
public class HealthCheckEndpointShould
{
    private readonly HttpClient _client;

    public HealthCheckEndpointShould(AgendaApplicationFixture fixture)
    {
        _client = fixture.ApiClient;
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
