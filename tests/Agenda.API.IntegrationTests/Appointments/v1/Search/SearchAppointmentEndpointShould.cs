//
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Linq.Expressions;
// using System.Net.Http;
// using System.Net.Http.Json;
// using System.Text.Json;
// using System.Threading.Tasks;
// using Agenda.API.IntegrationTests.Fixtures;
// using Agenda.API.Resources;
// using Agenda.API.Resources.Appointments;
// using Agenda.API.Resources.Appointments.v1.Create;
// using Agenda.API.Resources.v1.Appointments;
// using Agenda.Ids;
// using Agenda.Objects;
// using Bogus;
// using Candoumbe.DataAccess.Abstractions;
// using FluentAssertions;
// using FluentAssertions.Extensions;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Options;
// using NodaTime.Extensions;
// using Xunit;
// using Xunit.Abstractions;
// using Xunit.Categories;
//
// namespace Agenda.API.IntegrationTests.Appointments.v1.Search;
// [IntegrationTest]
// public class SearchAppointmentEndpointShould : IClassFixture<AgendaWebApplicationFactory>, IAsyncLifetime
// {
//     private readonly HttpClient _client;
//     private readonly ITestOutputHelper _outputHelper;
//     private readonly AgendaWebApplicationFactory _applicationFactory;
//     private static readonly Faker s_faker = new();
//     private readonly JsonSerializerOptions _jsonSerializerOptions;
//     public SearchAppointmentEndpointShould(ITestOutputHelper outputHelper,
//                                            AgendaWebApplicationFactory applicationFactory)
//     {
//         _client = applicationFactory.CreateClient();
//         _outputHelper = outputHelper;
//         _applicationFactory = applicationFactory;
//         _jsonSerializerOptions = _applicationFactory.Services
//                                                    .GetRequiredService<IOptions<JsonOptions>>()
//                                                    .Value.JsonSerializerOptions;
//     }
//
//     ///<inheritdoc/>
//     public Task InitializeAsync() => Task.CompletedTask;
//
//     ///<inheritdoc/>
//     public async Task DisposeAsync()
//     {
//         IUnitOfWorkFactory unitOfWorkFactory = _applicationFactory.Services.GetRequiredService<IUnitOfWorkFactory>();
//         using IUnitOfWork unitOfWork = unitOfWorkFactory.NewUnitOfWork();
//
//         await unitOfWork.Repository<Appointment>().Clear().ConfigureAwait(false);
//         await unitOfWork.SaveChangesAsync().ConfigureAwait(false);
//     }
//
//     public static IEnumerable<object[]> SearchAppointmentCases
//     {
//         get
//         {
//             yield return new object[]
//             {
//                 Enumerable.Empty<NewAppointmentInfo>(),
//                 "page=1&pageSize=10",
//                 (Expression<Func<HttpResponseMessage, bool>>)(response => response.StatusCode == System.Net.HttpStatusCode.OK),
//                 (Expression<Func<PageOf<Browsable<AppointmentInfo>>, bool>>)(page => page.Page == 1
//                                                                                      && page.PageSize == 10
//                                                                                      && page.Count == 0
//                                                                                      && page.Total == 0
//                                                                                      && page.Items.Exactly(0)),
//             };
//
//             {
//                 AppointmentId appointmentId = AppointmentId.New();
//                 yield return new object[]
//                 {
//                     new[]
//                     {
//                         new NewAppointmentInfo
//                         {
//                             Id = appointmentId,
//                             Subject = s_faker.Lorem.Sentence(),
//                             StartDate = 12.July(2019).Add(14.Hours()).AsUtc().ToInstant().InUtc().ToOffsetDateTime(),
//                             EndDate = 12.July(2019).Add(14.Hours().And(30.Minutes())).AsUtc().ToInstant().InUtc().ToOffsetDateTime(),
//                             Attendees = s_faker.Make(2, () => new AttendeeInfo
//                             {
//                                 Id = AttendeeId.New(),
//                                 Name = s_faker.Name.FullName(),
//                                 Email= s_faker.Person.Email,
//                                 PhoneNumber = s_faker.Person.Phone
//                             })
//                         }
//                     },
//                     "page=1&pageSize=10",
//                     (Expression<Func<HttpResponseMessage, bool>>)(response => response.StatusCode == System.Net.HttpStatusCode.OK),
//                     (Expression<Func<PageOf<Browsable<AppointmentInfo>>, bool>>)(page => page.Page == 1
//                                                                                             && page.PageSize == 10
//                                                                                             && page.Total == 1
//                                                                                             && page.Count == 1
//                                                                                             && page.Items.Once()
//                                                                                             && page.Items.Once(item => item.Resource.Id == appointmentId)),
//                 };
//             }
//
//             //{
//             //    AppointmentId appointmentId = AppointmentId.New();
//             //    yield return new object[]
//             //    {
//             //        new[]
//             //        {
//             //            new NewAppointmentInfo
//             //            {
//             //                Id = appointmentId,
//             //                Subject = Faker.Lorem.Sentence(),
//             //                StartDate = 12.July(2019).Add(14.Hours()).AsUtc().ToInstant().InUtc().ToOffsetDateTime(),
//             //                EndDate = 12.July(2019).Add(14.Hours().And(30.Minutes())).AsUtc().ToInstant().InUtc().ToOffsetDateTime(),
//             //                Attendees = Faker.Make(2, () => new AttendeeInfo
//             //                {
//             //                    Id = AttendeeId.New(),
//             //                    Name = Faker.Name.FullName(),
//             //                    Email= Faker.Person.Email,
//             //                    PhoneNumber = Faker.Person.Phone
//             //                })
//             //            }
//             //        },
//             //        "page=1&pageSize=10&from=2015-01-10T12:00:00Z&to=2019-07-12T14:30:00Z",
//             //        (Expression<Func<HttpResponseMessage, bool>>)(response => response.StatusCode == System.Net.HttpStatusCode.OK),
//             //        (Expression<Func<PageOf<Browsable<AppointmentInfo>>, bool>>)(page => page.Page == 1
//             //                                                                                && page.PageSize == 10
//             //                                                                                && page.Total == 0
//             //                                                                                && page.Count == 0
//             //                                                                                && page.Items.Exactly(0)),
//             //    };
//             //}
//
//             //{
//             //    AppointmentId appointmentId = AppointmentId.New();
//             //    yield return new object[]
//             //    {
//             //        new[]
//             //        {
//             //            new NewAppointmentInfo
//             //            {
//             //                Id = appointmentId,
//             //                Subject = Faker.Lorem.Sentence(),
//             //                StartDate = 12.July(2019).Add(14.Hours()).AsUtc().ToInstant().InUtc().ToOffsetDateTime(),
//             //                EndDate = 12.July(2019).Add(14.Hours().And(30.Minutes())).AsUtc().ToInstant().InUtc().ToOffsetDateTime(),
//             //                Attendees = Faker.Make(2, () => new AttendeeInfo
//             //                {
//             //                    Id = AttendeeId.New(),
//             //                    Name = Faker.Name.FullName(),
//             //                    Email= Faker.Person.Email,
//             //                    PhoneNumber = Faker.Person.Phone
//             //                })
//             //            }
//             //        },
//             //        "page=1&pageSize=10&from=2015-01-10T12:00:00Z&to=2019-07-19T14:30:00Z",
//             //        (Expression<Func<HttpResponseMessage, bool>>)(response => response.StatusCode == System.Net.HttpStatusCode.OK),
//             //        (Expression<Func<PageOf<Browsable<AppointmentInfo>>, bool>>)(page => page.Page == 1
//             //                                                                                && page.PageSize == 10
//             //                                                                                && page.Total == 0
//             //                                                                                && page.Count == 0
//             //                                                                                && page.Items.Exactly(0)),
//             //    };
//             //}
//         }
//     }
//
//     [Theory]
//     [MemberData(nameof(SearchAppointmentCases))]
//     public async Task Should_return_response_which_matches_expectation(IEnumerable<NewAppointmentInfo> initialData,
//                                                                        string search,
//                                                                        Expression<Func<HttpResponseMessage, bool>> responseExpectation,
//                                                                        Expression<Func<PageOf<Browsable<AppointmentInfo>>, bool>> pageExpectation)
//     {
//         // Arrange
//         foreach (NewAppointmentInfo newAppointmentInfo in initialData)
//         {
//             using HttpResponseMessage postResponse = await _client.PostAsJsonAsync("/appointments", newAppointmentInfo, _jsonSerializerOptions);
//             postResponse.EnsureSuccessStatusCode();
//         }
//
//         // Act
//         using HttpResponseMessage getResponse = await _client.GetAsync($"/appointments/?{search}");
//
//         _outputHelper.WriteLine($"GetResponse : '{await getResponse.Content.ReadAsStringAsync()}'");
//         // Assert
//         getResponse.Should().Match(responseExpectation);
//         PageOf<Browsable<AppointmentInfo>> page = await getResponse.Content.ReadFromJsonAsync<PageOf<Browsable<AppointmentInfo>>>(_jsonSerializerOptions);
//         page.Should().Match(pageExpectation);
//     }
// }