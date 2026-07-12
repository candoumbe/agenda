using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.IntegrationTests.Fixtures;
using Agenda.Ids;
using AwesomeAssertions;
using Bogus;
using Candoumbe.Forms;
using NodaTime;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Appointments.v1.Search;

[IntegrationTest]
[Feature(nameof(Appointments))]
public sealed class SearchAppointmentEndpointShould
{
    private readonly HttpClient _client;
    private readonly AgendaApplicationFixture _fixture;
    private string _accessToken;
    private static readonly Faker s_faker = new();

    public SearchAppointmentEndpointShould(AgendaApplicationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.ApiClient;
    }

    [Fact]
    public async Task Returns_an_empty_page_when_no_appointments_match_the_filter()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string uniqueSubjectFilter = $"no-match-{Guid.NewGuid():N}";
        string accessToken = await GetAccessTokenAsync(cancellationToken);

        // Act
        using HttpRequestMessage request = new(HttpMethod.Get, $"/appointments?page=1&pageSize=10&subject={Uri.EscapeDataString(uniqueSubjectFilter)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using HttpResponseMessage response = await _client.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        PageOf<Browsable<AppointmentInfo>> page = await response.Content.ReadFromJsonAsync<PageOf<Browsable<AppointmentInfo>>>(_fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(10);
        page.Count.Should().Be(0);
    }

    [Fact]
    public async Task Returns_the_created_appointment_when_searching_by_subject()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string accessToken = await GetAccessTokenAsync(cancellationToken);
        Instant startDate = s_faker.Noda().Instant.Soon();
        Instant endDate = s_faker.Noda().Instant.Future(reference: startDate);
        string uniqueSubject = $"subject-{Guid.NewGuid():N}";

        AppointmentInfo newAppointmentInfo = new()
        {
            Id = AppointmentId.New(),
            StartDate = startDate.InUtc().ToOffsetDateTime(),
            EndDate = endDate.InUtc().ToOffsetDateTime(),
            Location = s_faker.Address.City(),
            Attendees = [.. s_faker.Make(2, () => new AttendeeInfo
            {
                Name = s_faker.Name.FullName(),
                Email = s_faker.Internet.Email(),
                PhoneNumber = s_faker.Phone.PhoneNumber()
            })],
            Subject = uniqueSubject
        };

        using HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/appointments", newAppointmentInfo, _fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        Browsable<AppointmentInfo> createdAppointment = await createResponse.Content.ReadFromJsonAsync<Browsable<AppointmentInfo>>(_fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);
        createdAppointment.Should().NotBeNull();

        // Act
        using HttpRequestMessage searchRequest = new(HttpMethod.Get, $"/appointments?page=1&pageSize=10&subject={Uri.EscapeDataString(uniqueSubject)}");
        searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using HttpResponseMessage searchResponse = await _client.SendAsync(searchRequest, cancellationToken);

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PageOf<Browsable<AppointmentInfo>> page = await searchResponse.Content.ReadFromJsonAsync<PageOf<Browsable<AppointmentInfo>>>(_fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);
        page.Should().NotBeNull();

        // Assert
        page.Items.Should().Contain(item => item.Resource.Id == createdAppointment.Resource.Id);

        IEnumerable<Link> links = page.Items.First(item => item.Resource.Id == createdAppointment.Resource.Id).Links;
        links.Should()
            .OnlyContain(link => !string.IsNullOrWhiteSpace(link.Href))
              .And.OnlyContain(link => Uri.IsWellFormedUriString(link.Href, UriKind.Absolute) || Uri.IsWellFormedUriString(link.Href, UriKind.Relative));
    }

    [Fact]
    public async Task Returns_the_created_appointment_when_searching_by_subject_with_special_characters()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string accessToken = await GetAccessTokenAsync(cancellationToken);
        Instant startDate = s_faker.Noda().Instant.Soon();
        Instant endDate = s_faker.Noda().Instant.Future(reference: startDate);
        string uniqueSubject = $"subject special-'{Guid.NewGuid():N}";

        AppointmentInfo newAppointmentInfo = new()
        {
            Id = AppointmentId.New(),
            StartDate = startDate.InUtc().ToOffsetDateTime(),
            EndDate = endDate.InUtc().ToOffsetDateTime(),
            Location = s_faker.Address.City(),
            Attendees = [.. s_faker.Make(2, () => new AttendeeInfo
            {
                Name = s_faker.Name.FullName(),
                Email = s_faker.Internet.Email(),
                PhoneNumber = s_faker.Phone.PhoneNumber()
            })],
            Subject = uniqueSubject
        };

        using HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/appointments", newAppointmentInfo, _fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        Browsable<AppointmentInfo> createdAppointment = await createResponse.Content.ReadFromJsonAsync<Browsable<AppointmentInfo>>(_fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);
        createdAppointment.Should().NotBeNull();

        // Act
        using HttpRequestMessage searchRequest = new(HttpMethod.Get, $"/appointments?page=1&pageSize=10&subject={Uri.EscapeDataString(uniqueSubject)}");
        searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using HttpResponseMessage searchResponse = await _client.SendAsync(searchRequest, cancellationToken);

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PageOf<Browsable<AppointmentInfo>> page = await searchResponse.Content.ReadFromJsonAsync<PageOf<Browsable<AppointmentInfo>>>(_fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);
        page.Should().NotBeNull();

        // Assert
        page.Items.Should().Contain(item => item.Resource.Id == createdAppointment.Resource.Id);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            _accessToken = await _fixture.IssueAccessTokenAsync("alice", "password", cancellationToken);
        }

        return _accessToken;
    }

}
