//
// using System.Net;
// using System.Net.Http;
// using System.Net.Http.Json;
// using System.Threading.Tasks;
// using Agenda.API.IntegrationTests.Fixtures;
// using Agenda.API.Resources;
// using Agenda.API.Resources.Appointments;
// using Agenda.API.Resources.Appointments.v1.Create;
// using Agenda.API.Resources.v1.Appointments;
// using Agenda.Ids;
// using Bogus;
// using FluentAssertions;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Options;
// using NodaTime;
// using Projects;
// using Xunit;
// using Xunit.Abstractions;
// using Xunit.Categories;
//
// namespace Agenda.API.IntegrationTests.Appointments.v1.DeleteAnAppointment;
// [IntegrationTest]
// public class DeleteEndpointShould : IClassFixture<AgendaWebApplicationFactory<Agenda_AppHost>>
// {
//     private readonly HttpClient _client;
//     private static readonly Faker s_faker = new();
//     private readonly System.Text.Json.JsonSerializerOptions _jsonSerializerOptions;
//     private readonly ITestOutputHelper _outputHelper;
//     private readonly AgendaWebApplicationFactory _applicationFactory;
//
//     public DeleteEndpointShould(ITestOutputHelper outputHelper, AgendaWebApplicationFactory<Agenda_AppHost> applicationFactory)
//     {
//         _client = applicationFactory.CreateClient();
//         _outputHelper = outputHelper;
//         _applicationFactory = applicationFactory;
//         _jsonSerializerOptions = _applicationFactory.Services
//                                                    .GetRequiredService<IOptions<JsonOptions>>()
//                                                    .Value.JsonSerializerOptions;
//     }
//
//     [Fact]
//     public async Task Returns_NotFound_when_Id_does_not_exist()
//     {
//         // Act
//         HttpResponseMessage response = await _client.DeleteAsync($"/appointments/{AppointmentId.New()}");
//
//         // Assert
//         response.StatusCode.Should().Be(HttpStatusCode.NotFound, "the resource does not exist");
//     }
//
//     [Fact]
//     public async Task Returns_NoContent_when_Id_exists()
//     {
//         // Arrange
//         Instant startDate = s_faker.Noda().Instant.Soon();
//         Instant endDate = s_faker.Noda().Instant.Future(reference: startDate);
//
//         NewAppointmentInfo newAppointmentInfo = new()
//         {
//             Id = AppointmentId.New(),
//             StartDate = startDate.InUtc().ToOffsetDateTime(),
//             EndDate = endDate.InUtc().ToOffsetDateTime(),
//             Location = s_faker.Address.City(),
//             Attendees = s_faker.Make(2, action: () => new AttendeeInfo()
//             {
//                 Id = AttendeeId.New(),
//                 Name = s_faker.Name.FullName(),
//                 Email = s_faker.Internet.Email(),
//                 PhoneNumber = s_faker.Person.Phone
//             }),
//             Subject = s_faker.Lorem.Sentence()
//         };
//
//         using HttpResponseMessage createBrowsableResponse = await _client.PostAsJsonAsync("/appointments", newAppointmentInfo, _jsonSerializerOptions);
//         Browsable<AppointmentInfo> browsable = await createBrowsableResponse.Content.ReadFromJsonAsync<Browsable<AppointmentInfo>>(_jsonSerializerOptions);
//         string requestUri = $"/appointments/{browsable.Resource.Id}";
//
//         // Act
//         using HttpResponseMessage response = await _client.DeleteAsync(requestUri);
//
//         // Assert
//         response.StatusCode.Should()
//                            .Be(HttpStatusCode.NoContent);
//
//         using HttpResponseMessage getResponse = await _client.GetAsync(requestUri);
//
//         _outputHelper.WriteLine($"Content : {await getResponse.Content.ReadAsStringAsync()}");
//         getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
//     }
// }