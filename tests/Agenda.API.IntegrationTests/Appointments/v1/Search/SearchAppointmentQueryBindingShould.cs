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
public sealed class SearchAppointmentQueryBindingShould : IClassFixture<AgendaApplicationFixture>
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

        // Act
        using HttpResponseMessage response = await _client.GetAsync("/appointments?page=1&pageSize=10&from=2026-05-23T22:00:00.000Z&to=2026-06-08T21:59:59.999Z", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}