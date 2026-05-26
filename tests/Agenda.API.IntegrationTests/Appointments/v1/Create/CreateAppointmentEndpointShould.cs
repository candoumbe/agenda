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

namespace Agenda.API.IntegrationTests.Appointments.v1.Create;

[IntegrationTest]
[Feature(nameof(Appointments))]
public class CreateAppointmentEndpointShould
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _outputHelper;
    private readonly AgendaApplicationFixture _fixture;
    private static readonly Faker s_faker = new();

    public CreateAppointmentEndpointShould(AgendaApplicationFixture fixture, ITestOutputHelper outputHelper)
    {
        _fixture = fixture;
        _outputHelper = outputHelper;
        _client = fixture.ApiClient;
    }


    [Fact]
    public async Task Returns_the_appointment_when_created_successfully()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        _outputHelper.WriteLine("Client: " + _client.BaseAddress);
        Instant startDate = s_faker.Noda().Instant.Soon();
        Instant endDate = s_faker.Noda().Instant.Future(reference: startDate);

        AppointmentInfo newAppointmentInfo = new ()
        {
            Id = AppointmentId.New(),
            StartDate = startDate.InUtc().ToOffsetDateTime(),
            EndDate = endDate.InUtc().ToOffsetDateTime(),
            Location = s_faker.Address.City(),
            Attendees = [ ..s_faker.Make(2, () => new AttendeeInfo { Name = s_faker.Name.FullName(), Email = s_faker.Internet.Email(), PhoneNumber = s_faker.Phone.PhoneNumber() })],
            Subject = s_faker.Lorem.Sentence()
        };

        // Act
        using HttpResponseMessage response = await _client.PostAsJsonAsync("/appointments", newAppointmentInfo, _fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        Browsable<AppointmentInfo> browsable = await response.Content.ReadFromJsonAsync<Browsable<AppointmentInfo>>(_fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);

        IEnumerable<Link> links = browsable.Links;
        links.Should()
             .OnlyContain(link => !string.IsNullOrWhiteSpace(link.Href))
             .And.OnlyContain(link => Uri.IsWellFormedUriString(link.Href, UriKind.Absolute), "all links must be absolute URIs")
             .And.OnlyContain(link => link.Relations.AtLeastOnce())
             .And.Contain(link => link.Relations.Once(rel => rel == LinkRelation.Self))
             .And.Contain(link => link.Relations.Once(rel => string.Equals(rel, "delete", StringComparison.OrdinalIgnoreCase)));

        AppointmentInfo resource = browsable.Resource;
        resource.Id.Should().Be(newAppointmentInfo.Id);
        resource.Subject.Should().Be(newAppointmentInfo.Subject);
        resource.StartDate.Should().Be(newAppointmentInfo.StartDate);
        resource.EndDate.Should().Be(newAppointmentInfo.EndDate);
        resource.Attendees.Should().BeEquivalentTo(newAppointmentInfo.Attendees);
    }
}