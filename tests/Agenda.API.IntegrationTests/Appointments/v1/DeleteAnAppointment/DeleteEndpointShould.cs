namespace Agenda.API.IntegrationTests.Appointments.v1.DeleteAnAppointment;

using Agenda.API.IntegrationTests.Fixtures;
using Agenda.API.Resources;
using Agenda.API.Resources.Appointments.v1.Create;
using Agenda.API.Resources.v1.Appointments;
using Agenda.Ids;

using Bogus;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NodaTime;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

[IntegrationTest]
public class DeleteEndpointShould : IClassFixture<AgendaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly Faker Faker = new();
    private readonly System.Text.Json.JsonSerializerOptions JsonSerializerOptions;
    private readonly ITestOutputHelper _outputHelper;
    private readonly AgendaWebApplicationFactory _applicationFactory;

    public DeleteEndpointShould(ITestOutputHelper outputHelper, AgendaWebApplicationFactory applicationFactory)
    {
        _client = applicationFactory.CreateClient();
        _outputHelper = outputHelper;
        _applicationFactory = applicationFactory;
        JsonSerializerOptions = _applicationFactory.Services
                                                   .GetRequiredService<IOptions<JsonOptions>>()
                                                   .Value.JsonSerializerOptions;
    }

    [Fact]
    public async Task Returns_NotFound_when_Id_does_not_exist()
    {
        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"/appointments/{AppointmentId.New()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "the resource does not exist");
    }

    [Fact]
    public async Task Returns_NoContent_when_Id_exists()
    {
        // Arrange
        Instant startDate = Faker.Noda().Instant.Soon();
        Instant endDate = Faker.Noda().Instant.Future(reference: startDate);

        NewAppointmentInfo newAppointmentInfo = new()
        {
            Id = AppointmentId.New(),
            StartDate = startDate.InUtc().ToOffsetDateTime(),
            EndDate = endDate.InUtc().ToOffsetDateTime(),
            Location = Faker.Address.City(),
            Attendees = Faker.Make(2, action: () => new AttendeeInfo()
            {
                Id = AttendeeId.New(),
                Name = Faker.Name.FullName(),
                Email = Faker.Internet.Email(),
                PhoneNumber = Faker.Person.Phone
            }),
            Subject = Faker.Lorem.Sentence()
        };

        using HttpResponseMessage createBrowsableResponse = await _client.PostAsJsonAsync("/appointments", newAppointmentInfo, JsonSerializerOptions);
        Browsable<AppointmentInfo> browsable = await createBrowsableResponse.Content.ReadFromJsonAsync<Browsable<AppointmentInfo>>(JsonSerializerOptions);
        string requestUri = $"/appointments/{browsable.Resource.Id}";

        // Act
        using HttpResponseMessage response = await _client.DeleteAsync(requestUri);

        // Assert
        response.StatusCode.Should()
                           .Be(HttpStatusCode.NoContent);

        using HttpResponseMessage getResponse = await _client.GetAsync(requestUri);

        _outputHelper.WriteLine($"Content : {await getResponse.Content.ReadAsStringAsync()}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}