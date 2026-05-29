using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using AwesomeAssertions;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests;

[IntegrationTest]
public sealed class ApiDocumentationShould
{
    private readonly HttpClient _client;

    public ApiDocumentationShould(AgendaApplicationFixture fixture)
    {
        _client = fixture.ApiClient;
    }

    [Fact]
    public async Task Expose_api_documentation_from_scalar_endpoint()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        using HttpResponseMessage scalarResponse = await _client.GetAsync("/scalar", cancellationToken);
        using HttpResponseMessage versionedScalarResponse = await _client.GetAsync("/scalar/v1", cancellationToken);

        // Assert
        bool scalarEndpointExists = IsSuccessfulOrRedirectStatusCode(scalarResponse.StatusCode);
        bool versionedScalarEndpointExists = IsSuccessfulOrRedirectStatusCode(versionedScalarResponse.StatusCode);
        bool documentationExposedFromScalar = scalarEndpointExists || versionedScalarEndpointExists;

        documentationExposedFromScalar.Should().BeTrue(
            "API documentation should be served by Scalar from either /scalar or /scalar/v1");
    }

    [Fact]
    public async Task Not_expose_swagger_ui_endpoint_anymore()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        using HttpResponseMessage response = await _client.GetAsync("/swagger", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Swagger UI endpoint should not be available once Scalar migration is complete");
    }

    [Fact]
    public async Task Expose_openapi_document_for_v1()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        using HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "OpenAPI v1 JSON document should remain available for Scalar and API consumers");

        MediaTypeHeaderValue contentType = response.Content.Headers.ContentType
            ?? throw new InvalidOperationException("OpenAPI response should provide a content type header");
        contentType.MediaType.Should().Be("application/json");

        string payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using JsonDocument document = JsonDocument.Parse(payload);
        bool hasOpenApiVersion = document.RootElement.TryGetProperty("openapi", out JsonElement openApiProperty)
            && !string.IsNullOrWhiteSpace(openApiProperty.GetString());
        hasOpenApiVersion.Should().BeTrue("generated document should expose a valid OpenAPI version field");
    }

    private static bool IsSuccessfulOrRedirectStatusCode(HttpStatusCode statusCode)
    {
        bool isSuccessful = statusCode is >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices;
        bool isRedirect = statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

        bool result = isSuccessful || isRedirect;
        return result;
    }
}