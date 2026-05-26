using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using AwesomeAssertions;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests;

[IntegrationTest]
public class HealthCheckEndpointShould : IClassFixture<AgendaApplicationFixture>
{
    private readonly HttpClient _client;

    public HealthCheckEndpointShould(AgendaApplicationFixture fixture)
    {
        _client = fixture.ApiClient;
    }

    [Fact]
    public async Task Return_healthy_status_from_health_endpoint()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        using HttpResponseMessage response = await _client.GetAsync("/health", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
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
