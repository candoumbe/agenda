using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using AwesomeAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Authentication;

/// <summary>
/// Slow smoke test that validates the real Keycloak realm wiring end-to-end:
/// well-known discovery, password-grant token issuance, and token shape.
/// </summary>
[IntegrationTest]
[Trait("Category", "Smoke")]
public class KeycloakSmokeTests
{
    private readonly AgendaApplicationFixture _fixture;

    public KeycloakSmokeTests(AgendaApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Realm_publishes_well_known_metadata_and_mints_tokens_with_expected_audience_and_role()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act - well-known discovery
        using HttpResponseMessage discoveryResponse = await _fixture.KeycloakClient.GetAsync("/realms/agenda/.well-known/openid-configuration", cancellationToken);
        string discoveryPayload = await discoveryResponse.Content.ReadAsStringAsync(cancellationToken);

        // Act - direct access grant
        string accessToken = await _fixture.IssueAccessTokenAsync("alice", "password", cancellationToken);

        // Assert
        discoveryResponse.IsSuccessStatusCode.Should().BeTrue("the agenda realm must publish OpenID metadata");

        using JsonDocument discovery = JsonDocument.Parse(discoveryPayload);
        discovery.RootElement.GetProperty("issuer").GetString().Should().Contain("/realms/agenda");

        JsonWebToken parsed = new(accessToken);
        parsed.Audiences.Should().Contain("agenda-api");

        string realmAccess = parsed.GetClaim("realm_access")?.Value;
        realmAccess.Should().NotBeNullOrEmpty();
        using JsonDocument realmAccessDocument = JsonDocument.Parse(realmAccess);
        realmAccessDocument.RootElement.GetProperty("roles")
            .EnumerateArray()
            .Select(element => element.GetString())
            .Should()
            .Contain("agenda-user");
    }
}
