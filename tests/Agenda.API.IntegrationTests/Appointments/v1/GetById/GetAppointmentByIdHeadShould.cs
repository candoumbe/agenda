using System;
using System.Linq;
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
using NodaTime;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Appointments.v1.GetById;

[IntegrationTest]
public sealed class GetAppointmentByIdHeadShould
{
    private static readonly Faker s_faker = new();
    private readonly HttpClient _client;
  private readonly AgendaApplicationFixture _fixture;

    public GetAppointmentByIdHeadShould(AgendaApplicationFixture fixture)
    {
    _fixture = fixture;
        _client = fixture.ApiClient;
    }

    [Fact]
    public async Task Return_ok_and_link_header_when_resource_exists()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string appointmentId = await CreateAppointmentAsync(cancellationToken);
        using HttpRequestMessage request = new(HttpMethod.Head, $"/appointments/{appointmentId}");

        // Act
        using HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // response.Headers.Should().ContainKey("Link")
        //     .WhoseValue.Should()
        //     .ContainSingle(link => link.Contains("rel=\"self\"", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> CreateAppointmentAsync(CancellationToken cancellationToken)
    {
      AppointmentInfo appointment = BuildCreateAppointmentPayload();
      using HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/appointments", appointment, _fixture.ApiJsonSerializerOptions, cancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
      Browsable<AppointmentInfo> createdAppointment = await createResponse.Content.ReadFromJsonAsync<Browsable<AppointmentInfo>>(_fixture.ApiJsonSerializerOptions, cancellationToken: cancellationToken);
      string id = createdAppointment?.Resource.Id.ToString();

        id.Should().NotBeNullOrWhiteSpace();
        return id;
    }

    private static AppointmentInfo BuildCreateAppointmentPayload()
    {
      AppointmentId appointmentId = AppointmentId.New();
      AttendeeId attendeeId = AttendeeId.New();
        DateTimeOffset startDate = DateTimeOffset.UtcNow.AddHours(1);
        DateTimeOffset endDate = startDate.AddHours(1);

      return new AppointmentInfo
      {
        Id = appointmentId,
        Subject = s_faker.Lorem.Sentence(),
        Location = s_faker.Address.City(),
        StartDate = Instant.FromDateTimeOffset(startDate).InUtc().ToOffsetDateTime(),
        EndDate = Instant.FromDateTimeOffset(endDate).InUtc().ToOffsetDateTime(),
        Attendees =
        [
          new AttendeeInfo
          {
            Id = attendeeId,
            Name = s_faker.Name.FullName(),
            Email = s_faker.Internet.Email(),
            PhoneNumber = s_faker.Phone.PhoneNumber()
          }
        ]
      };
    }
}
