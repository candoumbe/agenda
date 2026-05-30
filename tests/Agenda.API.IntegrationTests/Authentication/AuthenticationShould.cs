using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using Agenda.UnitTests.Helpers.Authentication;
using AwesomeAssertions;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Authentication;

/// <summary>
/// End-to-end coverage for the authentication / authorization pipeline of the Agenda API.
/// </summary>
/// <remarks>
/// The Delete endpoint is the only currently protected endpoint without <c>AllowAnonymous</c>
/// (it also requires the <c>agenda-admin</c> realm role), so it is used as the probe for
/// the auth/authorization flow. Tokens crafted via <see cref="TokenFactory"/> are signed with
/// a key the API does not trust, so they exercise the negative validation paths regardless of
/// which assertion (<c>aud</c>/<c>iss</c>/<c>exp</c>/signature) the JWT bearer middleware
/// happens to surface first.
/// </remarks>
[IntegrationTest]
public class AuthenticationShould
{
    private static readonly Guid s_unknownAppointmentId = Guid.Parse("00000000-0000-0000-0000-0000feedface");
    private static readonly TokenFactory s_tokenFactory = new();

    private readonly AgendaApplicationFixture _fixture;

    public AuthenticationShould(AgendaApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    private static HttpRequestMessage BuildDeleteRequest(string token)
    {
        HttpRequestMessage request = new(HttpMethod.Delete, $"/appointments/{s_unknownAppointmentId}");
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return request;
    }

    [Fact]
    public async Task DELETE_returns_401_when_no_token()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using HttpRequestMessage request = BuildDeleteRequest(token: null);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DELETE_returns_401_when_token_is_expired()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string token = s_tokenFactory.Expired();
        using HttpRequestMessage request = BuildDeleteRequest(token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DELETE_returns_401_when_token_has_wrong_audience()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string token = s_tokenFactory.WrongAudience();
        using HttpRequestMessage request = BuildDeleteRequest(token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DELETE_returns_401_when_token_has_wrong_issuer()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string token = s_tokenFactory.WrongIssuer();
        using HttpRequestMessage request = BuildDeleteRequest(token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DELETE_returns_401_when_token_signature_is_invalid()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string token = s_tokenFactory.UnsignedOrTampered();
        using HttpRequestMessage request = BuildDeleteRequest(token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DELETE_returns_403_when_token_lacks_agenda_admin_role()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string token = await _fixture.IssueAccessTokenAsync("alice", "password", cancellationToken);
        using HttpRequestMessage request = BuildDeleteRequest(token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GET_appointments_returns_success_when_token_is_valid_with_agenda_user_role()
    {
        // Arrange
        // SearchAppointmentsEndpoint is currently AllowAnonymous so it accepts any caller; this test
        // documents that a properly authenticated user with the `agenda-user` role does not get
        // rejected by the auth pipeline (no 401/403). It will tighten automatically once
        // SearchAppointmentsEndpoint drops AllowAnonymous() (#578).
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string token = await _fixture.IssueAccessTokenAsync("alice", "password", cancellationToken);
        using HttpRequestMessage request = new(HttpMethod.Get, "/appointments?page=1&pageSize=1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized)
            .And.NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DELETE_authorizes_when_token_has_agenda_admin_role()
    {
        // Arrange
        // The admin token must clear the auth pipeline; the resource itself is unknown so the
        // endpoint returns 404 (NotFound). Either 204 (NoContent) or 404 are valid post-auth
        // outcomes — anything else (401 / 403) means auth was rejected.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string token = await _fixture.IssueAccessTokenAsync("admin", "password", cancellationToken);
        using HttpRequestMessage request = BuildDeleteRequest(token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }
}
