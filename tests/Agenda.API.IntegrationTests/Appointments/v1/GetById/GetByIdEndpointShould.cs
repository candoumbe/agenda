//
// using System;
// using System.Collections.Generic;
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
// using Candoumbe.Forms;
// using FluentAssertions;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Options;
// using NodaTime;
// using Xunit;
// using Xunit.Abstractions;
// using Xunit.Categories;
//
// namespace Agenda.API.IntegrationTests.Appointments.v1.GetById;
// [IntegrationTest]
// [Feature(nameof(Appointments))]
// public class GetByIdEndpointShould : IClassFixture<AgendaWebApplicationFactory>
// {
//     private readonly HttpClient _client;
//     private readonly ITestOutputHelper _outputHelper;
//     private static readonly Faker s_faker = new();
//     private readonly System.Text.Json.JsonSerializerOptions _jsonSerializerOptions;
//     private static readonly DateTimeZone s_defaultDateTimeZone = DateTimeZone.ForOffset(Offset.FromHours(2));
//
//     public GetByIdEndpointShould(ITestOutputHelper outputHelper, AgendaWebApplicationFactory applicationFactory)
//     {
//         _outputHelper = outputHelper;
//         _client = applicationFactory.CreateClient();
//         _client.DefaultRequestHeaders.Add(CurrentRequestMetadataInfoProvider.TimeZoneHeaderName, s_defaultDateTimeZone.Id);
//         _jsonSerializerOptions = applicationFactory.Services
//                                                    .GetRequiredService<IOptions<JsonOptions>>()
//                                                    .Value.JsonSerializerOptions;
//     }
//
//     [Fact]
//     public async Task Returns_NotFound_when_Id_does_not_exist()
//     {
//         // Arrange
//         AppointmentId appointmentId = AppointmentId.New();
//
//         // Act
//         using HttpResponseMessage getResponse = await _client.GetAsync($"/appointements/{appointmentId}");
//
//         getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
//     }
//
//     [Fact]
//     public async Task Returns_the_appointment_when_Id_exists()
//     {
//         // Arrange
//         ZonedDateTime startDate = s_faker.Noda().Instant.Soon().InZone(s_defaultDateTimeZone);
//         ZonedDateTime endDate = s_faker.Noda().Instant.Future(reference: startDate.ToInstant()).InZone(s_defaultDateTimeZone);
//
//         NewAppointmentInfo newAppointmentInfo = new()
//         {
//             Id = AppointmentId.New(),
//             StartDate = startDate.ToOffsetDateTime(),
//             EndDate = endDate.ToOffsetDateTime(),
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
//         using HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/appointments", newAppointmentInfo, _jsonSerializerOptions);
//
//         Browsable<AppointmentInfo> browsable = await createResponse.Content.ReadFromJsonAsync<Browsable<AppointmentInfo>>(_jsonSerializerOptions);
//
//         // Act
//         Browsable<GetAppointmentByIdResponse> browsableResult = await _client.GetFromJsonAsync<Browsable<GetAppointmentByIdResponse>>($"/appointments/{browsable.Resource.Id}", _jsonSerializerOptions);
//
//         // Assert
//         GetAppointmentByIdResponse resource = browsableResult.Resource;
//         resource.Id.Should().Be(newAppointmentInfo.Id);
//         //resource.StartDate.Should().Be(newAppointmentInfo.StartDate);
//         //resource.EndDate.Should().Be(newAppointmentInfo.EndDate);
//         resource.Subject.Should().Be(newAppointmentInfo.Subject);
//
//         IEnumerable<Link> links = browsableResult.Links;
//         links.Should().NotBeEmpty()
//                       .And.OnlyContain(link => !string.IsNullOrWhiteSpace(link.Href))
//                       .And.OnlyContain(link => Uri.IsWellFormedUriString(link.Href, UriKind.Absolute))
//                       .And.Contain(link => link.Relations.AtLeastOnce(rel => rel == LinkRelation.Self))
//                       ;
//     }
// }