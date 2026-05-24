using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        // Act
        using HttpResponseMessage response = await _client.GetAsync("/appointments?page=1&pageSize=10&from=2026-05-23T22:00:00.000Z&to=2026-06-08T21:59:59.999Z", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().Contain(header => string.Equals(header.Key, "Link", StringComparison.OrdinalIgnoreCase));
        response.Headers.Should().Contain(header => string.Equals(header.Key, "total", StringComparison.OrdinalIgnoreCase));
        response.Headers.Should().Contain(header => string.Equals(header.Key, "count", StringComparison.OrdinalIgnoreCase));

        string total = response.Headers.First(header => string.Equals(header.Key, "total", StringComparison.OrdinalIgnoreCase)).Value.Single();
        string count = response.Headers.First(header => string.Equals(header.Key, "count", StringComparison.OrdinalIgnoreCase)).Value.Single();

        total.Should().NotBeNullOrWhiteSpace();
        count.Should().NotBeNullOrWhiteSpace();
    }
}