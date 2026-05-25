using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.IntegrationTests.Fixtures;
using Agenda.Ids;
using Aspire.Hosting;
using AwesomeAssertions;
using Bogus;
using Candoumbe.Forms;
using NodaTime;
using xRetry.v3;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Appointments.v1.Search;

[IntegrationTest]
[Feature(nameof(Appointments))]
[Collection("AgendaApplication")]
public sealed class SearchAppointmentEndpointShould
{
    private readonly HttpClient _client;
    private readonly AgendaApplicationFixture _fixture;
    private static readonly Faker s_faker = new();

    public SearchAppointmentEndpointShould(AgendaApplicationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.ApiClient;
    }

    [RetryFact(maxRetries: 3, delayBetweenRetriesMs: 2000, SkipExceptions = [typeof(DistributedApplicationException)])]
    public async Task Returns_an_empty_page_when_no_appointments_match_the_filter()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string uniqueSubjectFilter = $"no-match-{Guid.NewGuid():N}";

        // Act
        using HttpResponseMessage response = await _client.GetAsync($"/appointments?page=1&pageSize=10&subject={uniqueSubjectFilter}", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        PageOf<Browsable<AppointmentInfo>> page = await response.Content.ReadFromJsonAsync<PageOf<Browsable<AppointmentInfo>>>(_fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(10);
        page.Count.Should().Be(0);
    }

    [RetryFact(maxRetries: 3, delayBetweenRetriesMs: 2000, SkipExceptions = [typeof(DistributedApplicationException)])]
    public async Task Returns_the_created_appointment_when_searching_by_subject()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
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
        PageOf<Browsable<AppointmentInfo>> page = await WaitForAppointmentToBeSearchableAsync(uniqueSubject, createdAppointment!.Resource.Id, cancellationToken);

        // Assert
        page.Items.Should().Contain(item => item.Resource.Id == createdAppointment.Resource.Id);

        IEnumerable<Link> links = page.Items.First(item => item.Resource.Id == createdAppointment.Resource.Id).Links;
        links.Should()
            .OnlyContain(link => !string.IsNullOrWhiteSpace(link.Href))
              .And.OnlyContain(link => Uri.IsWellFormedUriString(link.Href, UriKind.Absolute) || Uri.IsWellFormedUriString(link.Href, UriKind.Relative));
    }

    private async Task<PageOf<Browsable<AppointmentInfo>>> WaitForAppointmentToBeSearchableAsync(string uniqueSubject, AppointmentId appointmentId, CancellationToken cancellationToken)
    {
        PageOf<Browsable<AppointmentInfo>> page = null;

        HttpStatusCode? lastStatusCode = null;

        for (int attempt = 0; attempt < 120; attempt++)
        {
            using HttpResponseMessage response = await _client.GetAsync($"/appointments?page=1&pageSize=10&subject={Uri.EscapeDataString(uniqueSubject)}", cancellationToken);
            lastStatusCode = response.StatusCode;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                continue;
            }

            page = await response.Content.ReadFromJsonAsync<PageOf<Browsable<AppointmentInfo>>>(_fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);
            page.Should().NotBeNull();

            bool appointmentFound = page!.Items.Any(item => item.Resource.Id == appointmentId);
            if (appointmentFound)
            {
                return page;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        lastStatusCode.Should().Be(HttpStatusCode.OK);

        return page!;
    }
}
