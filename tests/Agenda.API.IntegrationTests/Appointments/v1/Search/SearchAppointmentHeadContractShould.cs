using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.v1.Appointments;
using Agenda.API.IntegrationTests.Fixtures;
using Agenda.Ids;
using Aspire.Hosting;
using AwesomeAssertions;
using Bogus;
using Candoumbe.Forms;
using DataFilters.Converters;
using Json.More;
using Json.Patch;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Serialization.SystemTextJson;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Appointments.v1.Search;

[IntegrationTest]
[Feature(nameof(Appointments))]
public sealed class SearchAppointmentHeadContractShould
{
    private readonly HttpClient _client;
    private static readonly Faker s_faker = new();
    private static readonly JsonSerializerOptions s_jsonSerializerOptions;

    static SearchAppointmentHeadContractShould()
    {
        s_jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true
        };

        s_jsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        s_jsonSerializerOptions.Converters.Add(new MultiFilterConverter());
        s_jsonSerializerOptions.Converters.Add(new FilterConverter());
        s_jsonSerializerOptions.Converters.Add(new PatchJsonConverter());
        s_jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<OperationType>());
        s_jsonSerializerOptions.Converters.Add(new EnumStringConverter<OperationType>());
        s_jsonSerializerOptions.Converters.Add(new AppointmentId.AppointmentIdSystemTextJsonConverter());
        s_jsonSerializerOptions.Converters.Add(new AttendeeId.AttendeeIdSystemTextJsonConverter());
    }

    public SearchAppointmentHeadContractShould(AgendaApplicationFixture fixture)
    {
        _client = fixture.ApiClient;
    }

    [Fact]
    public async Task Return_headers_for_head_with_pagination_contract_semantics()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Instant start = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromHours(2));

        await CreateAppointmentAsync(start, "Planning sync", "Paris", cancellationToken);
        await CreateAppointmentAsync(start.Plus(Duration.FromHours(2)), "Backlog review", "Paris", cancellationToken);
        await CreateAppointmentAsync(start.Plus(Duration.FromHours(4)), "Release checkpoint", "Paris", cancellationToken);

        string query = "/appointments?page=1&pageSize=2";

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
        string expectedTotalCount = root.GetProperty("totalCount").GetInt64().ToString(CultureInfo.InvariantCulture);

        if (headResponse.Headers.TryGetValues("count", out IEnumerable<string> headCountValues)
            && headResponse.Headers.TryGetValues("total", out IEnumerable<string> headTotalValues)
            && headResponse.Headers.TryGetValues("totalCount", out IEnumerable<string> headTotalCountValues))
        {
            headCountValues.Should().ContainSingle().Which.Should().Be(expectedCount);
            headTotalValues.Should().ContainSingle().Which.Should().Be(expectedTotal);
            headTotalCountValues.Should().ContainSingle().Which.Should().Be(expectedTotalCount);
        }

        if (headResponse.Headers.TryGetValues("Link", out IEnumerable<string> linkValues))
        {
            linkValues.Should().ContainSingle(link => link.Contains("rel=\"first\"", StringComparison.OrdinalIgnoreCase));
            linkValues.Should().ContainSingle(link => link.Contains("rel=\"last\"", StringComparison.OrdinalIgnoreCase));
            linkValues.Should().ContainSingle(link => link.Contains("rel=\"next\"", StringComparison.OrdinalIgnoreCase));
            linkValues.Should().NotContain(link => link.Contains("rel=\"previous\"", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task Accept_same_query_syntax_for_head_as_get_with_filters_and_pagination()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Instant matchingStart = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromHours(4));

        await CreateAppointmentAsync(matchingStart, "Backend design review", "Paris", cancellationToken);
        await CreateAppointmentAsync(matchingStart.Plus(Duration.FromDays(3)), "Frontend design review", "Lyon", cancellationToken);

        string subjectFilter = Uri.EscapeDataString("*design*");
        string locationFilter = Uri.EscapeDataString("*Paris*");
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
        string expectedTotalCount = root.GetProperty("totalCount").GetInt64().ToString(CultureInfo.InvariantCulture);

        if (headResponse.Headers.TryGetValues("count", out IEnumerable<string> headCountValues)
            && headResponse.Headers.TryGetValues("total", out IEnumerable<string> headTotalValues)
            && headResponse.Headers.TryGetValues("totalCount", out IEnumerable<string> headTotalCountValues))
        {
            headCountValues.Should().ContainSingle().Which.Should().Be(expectedCount);
            headTotalValues.Should().ContainSingle().Which.Should().Be(expectedTotal);
            headTotalCountValues.Should().ContainSingle().Which.Should().Be(expectedTotalCount);
        }
    }

    private async Task CreateAppointmentAsync(Instant startDate, string subject, string location, CancellationToken cancellationToken)
    {
        AppointmentInfo appointment = new()
        {
            Id = AppointmentId.New(),
            StartDate = startDate.InUtc().ToOffsetDateTime(),
            EndDate = startDate.Plus(Duration.FromMinutes(45)).InUtc().ToOffsetDateTime(),
            Subject = subject,
            Location = location,
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

        using HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/appointments", appointment, s_jsonSerializerOptions, cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}

