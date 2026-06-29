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
/// Security coverage for the appointments endpoints after <c>AllowAnonymous</c> removal (issue #623).
/// </summary>
/// <remarks>
/// Validates that Create (POST /appointments), GetById (GET /appointments/{id}),
/// Search (GET /appointments), and Patch (PATCH /appointments/{id}) all require a valid
/// bearer token and return 401 for unauthenticated or invalidly-authenticated requests.
/// Tokens crafted via <see cref="TokenFactory"/> are signed with a key the API does not
/// trust, so they exercise the negative validation paths regardless of which assertion
/// (<c>aud</c>/<c>iss</c>/<c>exp</c>/signature) the JWT bearer middleware surfaces first.
/// </remarks>
[IntegrationTest]
public sealed class AppointmentsEndpointsAuthorizationShould(AgendaApplicationFixture fixture)
{
    private static readonly Guid s_unknownAppointmentId = Guid.Parse("00000000-0000-0000-0000-0000feedface");
    private static readonly TokenFactory s_tokenFactory = new();

    private readonly AgendaApplicationFixture _fixture = fixture;

    /// <summary>
    /// Invalid token cases exercising the authentication pipeline negative paths.
    /// </summary>
    public static TheoryData<string> InvalidTokenCases =>
    [
        s_tokenFactory.Expired(),
        s_tokenFactory.WrongAudience(),
        s_tokenFactory.WrongIssuer(),
        s_tokenFactory.UnsignedOrTampered(),
    ];

    // -------------------------------------------------------------------------
    // POST /appointments
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Return_401_on_POST_appointments_when_no_token()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using HttpRequestMessage request = new(HttpMethod.Post, "/appointments")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(InvalidTokenCases))]
    public async Task Return_401_on_POST_appointments_when_token_is_invalid(string token)
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using HttpRequestMessage request = new(HttpMethod.Post, "/appointments")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Return_200_on_POST_appointments_when_user_is_authenticated()
    {
        // Arrange
        // A valid token clears the auth pipeline; an empty body triggers validation (400),
        // which is the expected non-auth outcome — anything other than 401/403 means auth passed.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string token = await _fixture.IssueAccessTokenAsync("alice", "password", cancellationToken);
        using HttpRequestMessage request = new(HttpMethod.Post, "/appointments")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized)
            .And.NotBe(HttpStatusCode.Forbidden);
    }

    // -------------------------------------------------------------------------
    // GET /appointments/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Return_401_on_GET_appointment_by_id_when_no_token()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using HttpRequestMessage request = new(HttpMethod.Get, $"/appointments/{s_unknownAppointmentId}");

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(InvalidTokenCases))]
    public async Task Return_401_on_GET_appointment_by_id_when_token_is_invalid(string token)
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using HttpRequestMessage request = new(HttpMethod.Get, $"/appointments/{s_unknownAppointmentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Return_200_on_GET_appointment_by_id_when_user_is_authenticated()
    {
        // Arrange
        // A valid token clears the auth pipeline; an unknown id produces 404 (NotFound),
        // which is the expected non-auth outcome — anything other than 401/403 means auth passed.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string token = await _fixture.IssueAccessTokenAsync("alice", "password", cancellationToken);
        using HttpRequestMessage request = new(HttpMethod.Get, $"/appointments/{s_unknownAppointmentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized)
            .And.NotBe(HttpStatusCode.Forbidden);
    }

    // -------------------------------------------------------------------------
    // GET /appointments (Search)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Return_401_on_GET_appointments_when_no_token()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using HttpRequestMessage request = new(HttpMethod.Get, "/appointments?page=1&pageSize=1");

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(InvalidTokenCases))]
    public async Task Return_401_on_GET_appointments_when_token_is_invalid(string token)
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using HttpRequestMessage request = new(HttpMethod.Get, "/appointments?page=1&pageSize=1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Return_200_on_GET_appointments_when_user_is_authenticated()
    {
        // Arrange
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

    // -------------------------------------------------------------------------
    // PATCH /appointments/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Return_401_on_PATCH_appointment_when_no_token()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using HttpRequestMessage request = new(HttpMethod.Patch, $"/appointments/{s_unknownAppointmentId}")
        {
            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json-patch+json")
        };

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(InvalidTokenCases))]
    public async Task Return_401_on_PATCH_appointment_when_token_is_invalid(string token)
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using HttpRequestMessage request = new(HttpMethod.Patch, $"/appointments/{s_unknownAppointmentId}")
        {
            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json-patch+json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Return_200_on_PATCH_appointment_when_user_is_authenticated()
    {
        // Arrange
        // A valid token clears the auth pipeline; an unknown id produces 404 (NotFound),
        // which is the expected non-auth outcome — anything other than 401/403 means auth passed.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string token = await _fixture.IssueAccessTokenAsync("alice", "password", cancellationToken);
        using HttpRequestMessage request = new(HttpMethod.Patch, $"/appointments/{s_unknownAppointmentId}")
        {
            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json-patch+json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        using HttpResponseMessage response = await _fixture.AnonymousApiClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should()
            .NotBe(HttpStatusCode.Unauthorized)
            .And.NotBe(HttpStatusCode.Forbidden);
    }
}
