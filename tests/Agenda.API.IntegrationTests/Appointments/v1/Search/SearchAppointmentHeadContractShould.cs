using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.v1.Appointments;
using Agenda.API.IntegrationTests.Fixtures;
using Aspire.Hosting;
using AwesomeAssertions;
using Bogus;
using NodaTime;
using xRetry.v3;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Appointments.v1.Search;

[IntegrationTest]
[Feature(nameof(Appointments))]
public sealed class SearchAppointmentHeadContractShould(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    private HttpClient _client;
    private AgendaApplicationTestingBuilder _appHost;
    private static readonly Faker s_faker = new();

    public async ValueTask InitializeAsync()
    {
        _appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(outputHelper, TestContext.Current.CancellationToken);
        await _appHost.StartAsync(TestContext.Current.CancellationToken);
        _client = _appHost.ApiClient;
    }

    public async ValueTask DisposeAsync()
    {
        await _appHost.DisposeAsync();
    }

    [RetryFact(maxRetries: 3, delayBetweenRetriesMs: 2000, SkipExceptions = [typeof(DistributedApplicationException)])]
    public async Task Return_headers_for_head_with_pagination_contract_semantics()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Instant start = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromHours(1));

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

        GetSingleHeaderValue(headResponse, "count").Should().Be(expectedCount);
        GetSingleHeaderValue(headResponse, "total").Should().Be(expectedTotal);
        GetSingleHeaderValue(headResponse, "totalCount").Should().Be(expectedTotalCount);

        IEnumerable<string> linkValues = GetHeaderValues(headResponse, "Link");
        linkValues.Should().ContainSingle(link => link.Contains("rel=\"first\"", StringComparison.OrdinalIgnoreCase));
        linkValues.Should().ContainSingle(link => link.Contains("rel=\"last\"", StringComparison.OrdinalIgnoreCase));
        linkValues.Should().ContainSingle(link => link.Contains("rel=\"next\"", StringComparison.OrdinalIgnoreCase));
        linkValues.Should().NotContain(link => link.Contains("rel=\"previous\"", StringComparison.OrdinalIgnoreCase));
    }

    [RetryFact(maxRetries: 3, delayBetweenRetriesMs: 2000, SkipExceptions = [typeof(DistributedApplicationException)])]
    public async Task Accept_same_query_syntax_for_head_as_get_with_filters_and_pagination()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Instant matchingStart = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromHours(2));
        Instant from = matchingStart.Minus(Duration.FromHours(1));
        Instant to = matchingStart.Plus(Duration.FromDays(15));

        await CreateAppointmentAsync(matchingStart, "Backend design review", "Paris", cancellationToken);
        await CreateAppointmentAsync(matchingStart.Plus(Duration.FromDays(3)), "Frontend design review", "Lyon", cancellationToken);

        string subjectFilter = Uri.EscapeDataString("*design*");
        string locationFilter = Uri.EscapeDataString("*Paris*");
        string query = $"/appointments?page=1&pageSize=10&subject={subjectFilter}&location={locationFilter}&from={ToUtcQueryTimestamp(from)}&to={ToUtcQueryTimestamp(to)}";

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

        GetSingleHeaderValue(headResponse, "count").Should().Be(expectedCount);
        GetSingleHeaderValue(headResponse, "total").Should().Be(expectedTotal);
        GetSingleHeaderValue(headResponse, "totalCount").Should().Be(expectedTotalCount);
    }

    private async Task CreateAppointmentAsync(Instant startDate, string subject, string location, CancellationToken cancellationToken)
    {
                string appointmentId = Guid.NewGuid().ToString();
                string attendeeId = Guid.NewGuid().ToString();
                DateTimeOffset startDateTime = startDate.ToDateTimeOffset();
                DateTimeOffset endDateTime = startDate.Plus(Duration.FromMinutes(45)).ToDateTimeOffset();

                string payload = $$"""
                                                 {
                                                     "id": "{{appointmentId}}",
                                                     "subject": "{{subject}}",
                                                     "location": "{{location}}",
                                                     "startDate": "{{startDateTime:O}}",
                                                     "endDate": "{{endDateTime:O}}",
                                                     "attendees": [
                                                         {
                                                             "id": "{{attendeeId}}",
                                                             "name": "{{s_faker.Name.FullName()}}",
                                                             "email": "{{s_faker.Internet.Email()}}",
                                                             "phoneNumber": "{{s_faker.Phone.PhoneNumber()}}"
                                                         }
                                                     ]
                                                 }
                                                 """;

                using StringContent content = new(payload, Encoding.UTF8, "application/json");
                using HttpResponseMessage createResponse = await _client.PostAsync("/appointments", content, cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static string GetSingleHeaderValue(HttpResponseMessage response, string headerName)
    {
        response.Headers.TryGetValues(headerName, out IEnumerable<string> values).Should().BeTrue($"{headerName} header should exist");
        values.Should().NotBeNull();
        values.Should().ContainSingle();

        return values.Single();
    }

    private static IEnumerable<string> GetHeaderValues(HttpResponseMessage response, string headerName)
    {
        response.Headers.TryGetValues(headerName, out IEnumerable<string> values).Should().BeTrue($"{headerName} header should exist");
        values.Should().NotBeNull();

        return values;
    }

    private static string ToUtcQueryTimestamp(Instant instant)
    {
        return instant.ToDateTimeUtc().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
    }
}
