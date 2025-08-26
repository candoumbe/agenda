using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using Agenda.API.Resources;
using Agenda.API.Resources.Appointments;
using Agenda.API.Resources.Appointments.v1.Create;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Bogus;
using Candoumbe.Forms;
using DataFilters.Converters;
using FluentAssertions;
using Fluxera.StronglyTypedId.SystemTextJson;
using Json.More;
using Json.Patch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Projects;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Agenda.API.IntegrationTests.Appointments.v1.Create;

[IntegrationTest]
[Feature(nameof(Appointments))]
public class CreateAppointmentEndpointShould : IAsyncLifetime
{
    private HttpClient _client;
    private static readonly Faker s_faker = new();
    private AgendaApplicationTestingBuilder _appHost;
    private static readonly JsonSerializerOptions s_jsonSerializerOptions;
    private DistributedApplication _sut;
    private readonly ITestOutputHelper _outputHelper;

    static CreateAppointmentEndpointShould()
    {
        s_jsonSerializerOptions = new JsonSerializerOptions();

        //s.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        s_jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        s_jsonSerializerOptions.AllowTrailingCommas = true;
        s_jsonSerializerOptions.UseStronglyTypedId();
        s_jsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        s_jsonSerializerOptions.Converters.Add(new MultiFilterConverter());
        s_jsonSerializerOptions.Converters.Add(new FilterConverter());
        s_jsonSerializerOptions.Converters.Add(new PatchJsonConverter());
        s_jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<OperationType>());
        s_jsonSerializerOptions.Converters.Add(new EnumStringConverter<OperationType>());
    }

    public CreateAppointmentEndpointShould(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    private static readonly TimeSpan BuildStopTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan StartStopTimeout = TimeSpan.FromSeconds(120);


    ///<inheritdoc/>
    public async Task InitializeAsync()
    {
        _appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(_outputHelper);

        _sut = await _appHost.StartAsync(StartStopTimeout, BuildStopTimeout);

        await _sut.StartAsync().WaitAsync(StartStopTimeout);
        await _sut.WaitForResourcesAsync().WaitAsync(StartStopTimeout);
    }

    ///<inheritdoc/>
    public async Task DisposeAsync()
    {
        await _sut.StopAsync().WaitAsync(BuildStopTimeout);
    }


    [Fact]
    public async Task Returns_the_appointment_when_created_successfully()
    {
        // Arrange
        _client = _sut.CreateHttpClient("api");
        _outputHelper.WriteLine("Client: " + _client.BaseAddress);
        Instant startDate = s_faker.Noda().Instant.Soon();
        Instant endDate = s_faker.Noda().Instant.Future(reference: startDate);

        var newAppointmentInfo = new
        {
            Id = Guid.CreateVersion7(),
            StartDate = startDate.InUtc().ToOffsetDateTime(),
            EndDate = endDate.InUtc().ToOffsetDateTime(),
            Location = s_faker.Address.City(),
            Attendees = s_faker.Make(2, () => new { Id = Guid.CreateVersion7(), Name = s_faker.Name.FullName(), Email = s_faker.Internet.Email(), PhoneNumber = s_faker.Phone.PhoneNumber() }),
            Subject = s_faker.Lorem.Sentence()
        };

        // Act
        using HttpResponseMessage response = await _client.PostAsJsonAsync("/appointments", newAppointmentInfo, s_jsonSerializerOptions);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        Browsable<AppointmentInfo> browsable = await response.Content.ReadFromJsonAsync<Browsable<AppointmentInfo>>();

        IEnumerable<Link> links = browsable.Links;
        links.Should()
             .OnlyContain(link => !string.IsNullOrWhiteSpace(link.Href))
             .And.OnlyContain(link => Uri.IsWellFormedUriString(link.Href, UriKind.Absolute), "all links must be absolute URIs")
             .And.OnlyContain(link => link.Relations.AtLeastOnce())
             .And.Contain(link => link.Relations.Once(rel => rel == LinkRelation.Self))
             .And.Contain(link => link.Relations.Once(rel => string.Equals(rel, "delete", StringComparison.OrdinalIgnoreCase)))
             .And.Contain(link => link.Relations.Once(rel => string.Equals(rel, "attendees", StringComparison.OrdinalIgnoreCase)))
             ;

        AppointmentInfo resource = browsable.Resource;
        resource.Id.Should().Be(newAppointmentInfo.Id);
        resource.Subject.Should().Be(newAppointmentInfo.Subject);
        resource.StartDate.Should().Be(newAppointmentInfo.StartDate);
        resource.EndDate.Should().Be(newAppointmentInfo.EndDate);
        resource.Attendees.Should().BeEquivalentTo(newAppointmentInfo.Attendees);
    }
}