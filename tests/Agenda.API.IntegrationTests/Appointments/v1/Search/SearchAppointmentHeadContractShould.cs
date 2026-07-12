using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.v1.Appointments;
using Agenda.API.IntegrationTests.Fixtures;
using Agenda.Ids;
using AwesomeAssertions;
using Bogus;
using NodaTime;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Appointments.v1.Search;

[IntegrationTest]
[Feature(nameof(Appointments))]
public sealed class SearchAppointmentHeadContractShould
{
    private readonly HttpClient _client;
    private readonly AgendaApplicationFixture _fixture;
    private static readonly Faker s_faker = new();
    private readonly IClock _clock;
    
    public SearchAppointmentHeadContractShould(AgendaApplicationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.ApiClient;
        _clock = SystemClock.Instance;
    }

    [Fact]
    public async Task Return_headers_for_head_with_pagination_contract_semantics()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Instant start = _clock.GetCurrentInstant().Plus(Duration.FromDays(1));

        await CreateAppointmentAsync(start, "Planning sync", "Paris", cancellationToken);
        await CreateAppointmentAsync(start.Plus(Duration.FromHours(2)), "Backlog review", "Paris", cancellationToken);
        await CreateAppointmentAsync(start.Plus(Duration.FromHours(4)), "Release checkpoint", "Paris", cancellationToken);

        string query = "/appointments?page=1&pageSize=2";
        HttpRequestMessage request = new(HttpMethod.Head, query);

        // Act
        using HttpResponseMessage headResponse = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        // Assert
        headResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        HttpResponseHeaders headers = headResponse.Headers;
        headers.Should().ContainKey("total")
            .WhoseValue.Should().ContainSingle("3");
        headers.Should().ContainKey("count")
            .WhoseValue.Should().ContainSingle("2");
        headers.Should().NotContainKey("totalCount");

        headers.Should().ContainKey("Link")
            .WhoseValue.Should().ContainSingle(link => link.Contains("rel=\"first\"", StringComparison.OrdinalIgnoreCase))
            .And.ContainSingle(link => link.Contains("rel=\"first\"", StringComparison.OrdinalIgnoreCase))
            .And.ContainSingle(link => link.Contains("rel=\"last\"", StringComparison.OrdinalIgnoreCase))
            .And.ContainSingle(link => link.Contains("rel=\"next\"", StringComparison.OrdinalIgnoreCase));
        headers.Should().NotContain(header => header.Key.Equals("previous", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Accept_same_query_syntax_for_head_as_get_with_filters_and_pagination()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Instant matchingStart = _clock.GetCurrentInstant().Plus(Duration.FromDays(1));

        await CreateAppointmentAsync(matchingStart, "Backend design review", "Paris", cancellationToken);
        await CreateAppointmentAsync(matchingStart.Plus(Duration.FromDays(3)), "Frontend design review", "Lyon", cancellationToken);

        string subjectFilter = "*design*";
        string locationFilter = "*Paris*";
        DateTimeOffset from = matchingStart.Minus(Duration.FromMinutes(30)).ToDateTimeOffset();
        DateTimeOffset to = matchingStart.Plus(Duration.FromDays(7)).ToDateTimeOffset();
        string fromParam = Uri.EscapeDataString(from.ToString("O", CultureInfo.InvariantCulture));
        string toParam = Uri.EscapeDataString(to.ToString("O", CultureInfo.InvariantCulture));
        string query = $"/appointments?page=1&pageSize=10&subject={subjectFilter}&location={locationFilter}&from={fromParam}&to={toParam}";

        // Act
        using HttpResponseMessage getResponse = await _client.GetAsync(query, cancellationToken);
        using HttpResponseMessage headResponse = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, query), HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        headResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument getPayload = await JsonDocument.ParseAsync(await getResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        JsonElement root = getPayload.RootElement;

        string expectedCount = root.GetProperty("count").GetInt64().ToString(CultureInfo.InvariantCulture);
        string expectedTotal = root.GetProperty("total").GetInt64().ToString(CultureInfo.InvariantCulture);
        string expectedFirstRelation = "rel=\"first\"";
        string expectedLastRelation = "rel=\"last\"";

        getResponse.Headers.Should().ContainKey("count")
            .WhoseValue.Should().ContainSingle(expectedCount);
        getResponse.Headers.Should().ContainKey("total")
            .WhoseValue.Should().ContainSingle(expectedTotal);
        getResponse.Headers.Should().NotContainKey("totalCount");
        getResponse.Headers.Should().ContainKey("Link")
            .WhoseValue.Should().ContainSingle(link => link.Contains(expectedFirstRelation, StringComparison.OrdinalIgnoreCase))
            .And.ContainSingle(link => link.Contains(expectedLastRelation, StringComparison.OrdinalIgnoreCase));

        headResponse.Headers.Should().ContainKey("count")
            .WhoseValue.Should().ContainSingle(expectedCount);
        headResponse.Headers.Should().ContainKey("total")
            .WhoseValue.Should().ContainSingle(expectedTotal);
        headResponse.Headers.Should().NotContainKey("totalCount");
        headResponse.Headers.Should().ContainKey("Link")
            .WhoseValue.Should().ContainSingle(link => link.Contains(expectedFirstRelation, StringComparison.OrdinalIgnoreCase))
            .And.ContainSingle(link => link.Contains(expectedLastRelation, StringComparison.OrdinalIgnoreCase));
    }

    private async Task CreateAppointmentAsync(Instant startDate, string subject, string location, CancellationToken cancellationToken)
    {
        DateTimeOffset startDateTime = startDate.ToDateTimeOffset();
        DateTimeOffset endDateTime = startDate.Plus(Duration.FromMinutes(45)).ToDateTimeOffset();
        AppointmentInfo request = new()
        {
            Id = AppointmentId.New(),
            Subject = subject,
            Location = location,
            StartDate = Instant.FromDateTimeOffset(startDateTime).InUtc().ToOffsetDateTime(),
            EndDate = Instant.FromDateTimeOffset(endDateTime).InUtc().ToOffsetDateTime(),
            Attendees =
            [
                new AttendeeInfo
                {
                    Id = AttendeeId.New(),
                    Name = s_faker.Name.FullName(),
                    Email = s_faker.Internet.Email(),
                    PhoneNumber = s_faker.Phone.PhoneNumber()
                }
            ]
        };

        using HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/appointments", request, _fixture.ApiJsonSerializerOptions, cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}

