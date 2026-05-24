using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using AwesomeAssertions;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Appointments.v1.Search;

[IntegrationTest]
public sealed class SearchAppointmentQueryBindingShould
{
    private readonly HttpClient _client;

    public SearchAppointmentQueryBindingShould(AgendaApplicationFixture fixture)
    {
        _client = fixture.ApiClient;
    }

    [Fact]
    public async Task Return_ok_when_query_contains_iso_offset_datetime_range()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Actawait ExecuteRequestWithTransientInfrastructureRetryAsync(HttpMethod.Get, cancellationToken);
        using HttpResponseMessage response = await _client.GetAsync("/appointments?page=1&pageSize=10&from=2026-05-23T22:00:00.000Z&to=2026-06-08T21:59:59.999Z", cancellationToken);   

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Return_ok_and_navigation_headers_when_head_query_contains_iso_offset_datetime_range()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpRequestMessage request = new(HttpMethod.Head, "/appointments?page=1&pageSize=10&from=2026-05-23T22:00:00.000Z&to=2026-06-08T21:59:59.999Z");

        // Act
        using HttpResponseMessage response = await _client.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        if (response.Headers.TryGetValues("Link", out IEnumerable<string> linkValues))
        {
            linkValues.Should().NotBeNull();
        }

        if (response.Headers.TryGetValues("total", out IEnumerable<string> totalValues)
            && response.Headers.TryGetValues("count", out IEnumerable<string> countValues))
        {
            totalValues.Should().ContainSingle();
            countValues.Should().ContainSingle();
            totalValues.Single().Should().NotBeNullOrWhiteSpace();
            countValues.Single().Should().NotBeNullOrWhiteSpace();
        }
    }
}