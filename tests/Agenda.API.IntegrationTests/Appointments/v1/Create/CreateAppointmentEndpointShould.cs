namespace Agenda.API.IntegrationTests.Appointments.v1.Create;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Bogus;
using Candoumbe.Forms;
using Fixtures;
using FluentAssertions;
using Ids;
using Meziantou.Extensions.Logging.Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using Resources;
using Resources.Appointments.v1.Create;
using Resources.v1.Appointments;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;
using Xunit.Extensions.AssemblyFixture;

[IntegrationTest]
[Feature(nameof(Appointments))]
public class CreateAppointmentEndpointShould : IClassFixture<AgendaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _outputHelper;
    private readonly AgendaWebApplicationFactory _applicationFactory;
    private static readonly Faker Faker = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public CreateAppointmentEndpointShould(ITestOutputHelper outputHelper, AgendaWebApplicationFactory applicationFactory)
    {
        _outputHelper = outputHelper;

        applicationFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(new XUnitLoggerProvider(outputHelper)));
        });

        _applicationFactory = applicationFactory;
        _client = _applicationFactory.CreateClient();
        _jsonSerializerOptions = _applicationFactory.Services
                                                   .GetRequiredService<IOptions<JsonOptions>>()
                                                   .Value.JsonSerializerOptions;
    }

    [Fact]
    public async Task Returns_the_appointment_when_created_successfully()
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
            Attendees = Faker.Make(2, () => new AttendeeInfo
            {
                Id = AttendeeId.New(),
                Name = Faker.Person.FullName,
                Email = Faker.Person.Email,
                PhoneNumber = Faker.Person.Phone
            }),
            Subject = Faker.Lorem.Sentence()
        };

        // Act
        using HttpResponseMessage response = await _client.PostAsJsonAsync("/appointments", newAppointmentInfo, _jsonSerializerOptions);

        // Assert
        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);

        Browsable<AppointmentInfo> browsable = await response.Content.ReadFromJsonAsync<Browsable<AppointmentInfo>>(_jsonSerializerOptions);

        IEnumerable<Link> links = browsable.Links;
        links.Should()
             .OnlyContain(link => !string.IsNullOrWhiteSpace(link.Href))
             .And.OnlyContain(link => Uri.IsWellFormedUriString(link.Href, UriKind.Absolute), "all links must be absolute URIs")
             .And.OnlyContain(link => link.Relations.AtLeastOnce())
             .And.Contain(link => link.Relations.Once(rel => rel == LinkRelation.Self))
             .And.Contain(link => link.Relations.Once(rel => string.Equals(rel, "delete", StringComparison.OrdinalIgnoreCase)))
             //.And.Contain(link => link.Relations.Once(rel => string.Equals(rel, "attendees", StringComparison.OrdinalIgnoreCase)))
             ;

        AppointmentInfo resource = browsable.Resource;
        resource.Id.Should().Be(newAppointmentInfo.Id);
        resource.Subject.Should().Be(newAppointmentInfo.Subject);
        resource.StartDate.Should().Be(newAppointmentInfo.StartDate);
        resource.EndDate.Should().Be(newAppointmentInfo.EndDate);
        resource.Attendees.Should().BeEquivalentTo(newAppointmentInfo.Attendees);
    }
}
