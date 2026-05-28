using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.v1.Appointments;
using Agenda.API.IntegrationTests.Fixtures;
using Agenda.Ids;
using AwesomeAssertions;
using Bogus;
using Candoumbe.Forms;
using NodaTime;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Appointments.v1.GetById;

[IntegrationTest]
[Feature(nameof(Appointments))]
public sealed class GetByIdEndpointShould
{
    private readonly HttpClient _client;
    private readonly AgendaApplicationFixture _fixture;
    private static readonly Faker s_faker = new();

    public GetByIdEndpointShould(AgendaApplicationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.ApiClient;
    }

    [Fact]
    public async Task Returns_NotFound_when_Id_does_not_exist()
    {
        // Arrange
        AppointmentId appointmentId = AppointmentId.New();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        using HttpResponseMessage getResponse = await _client.GetAsync($"/appointments/{appointmentId}", cancellationToken);

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_the_appointment_when_Id_exists()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Instant startDate = s_faker.Noda().Instant.Future(reference: SystemClock.Instance.GetCurrentInstant());
        Instant endDate = s_faker.Noda().Instant.Future(reference: startDate);

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
            Subject = s_faker.Lorem.Sentence()
        };

        using HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/appointments", newAppointmentInfo, _fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        Browsable<AppointmentInfo> createdAppointment = await createResponse.Content.ReadFromJsonAsync<Browsable<AppointmentInfo>>(_fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);
        createdAppointment.Should().NotBeNull();

        // Act
        using HttpResponseMessage getResponse = await _client.GetAsync($"/appointments/{createdAppointment!.Resource.Id}", cancellationToken);

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse.Headers.Should().ContainKey("Link")
            .WhoseValue.Should().ContainSingle(link => link.Contains("rel=\"self\"", StringComparison.OrdinalIgnoreCase));

        Browsable<GetAppointmentByIdResponse> browsableResult = await getResponse.Content.ReadFromJsonAsync<Browsable<GetAppointmentByIdResponse>>(_fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);
        browsableResult.Should().NotBeNull();
        GetAppointmentByIdResponse resource = browsableResult!.Resource;

        resource.Id.Should().Be(newAppointmentInfo.Id);
        resource.Subject.Should().Be(newAppointmentInfo.Subject);
        resource.Location.Should().Be(newAppointmentInfo.Location);

        IEnumerable<Link> links = browsableResult.Links;
        links.Should()
            .OnlyContain(link => !string.IsNullOrWhiteSpace(link.Href))
            .And.OnlyContain(link => Uri.IsWellFormedUriString(link.Href, UriKind.Absolute) || Uri.IsWellFormedUriString(link.Href, UriKind.Relative))
            .And.Contain(link => link.Relations.Once(rel => rel == LinkRelation.Self));
    }
}
