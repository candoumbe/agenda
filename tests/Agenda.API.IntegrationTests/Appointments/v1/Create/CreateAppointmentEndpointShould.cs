using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.v1.Appointments;
using Agenda.API.IntegrationTests.Fixtures;
using Agenda.Ids;
using Aspire.Hosting;
using Bogus;
using Candoumbe.Forms;
using DataFilters.Converters;
using FluentAssertions;
using FluentAssertions.NodaTime;
using Fluxera.StronglyTypedId.SystemTextJson;
using Json.More;
using Json.Patch;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Appointments.v1.Create;

[IntegrationTests]
[Feature(nameof(Appointments))]
public class CreateAppointmentEndpointShould(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    private HttpClient _client;
    private static readonly Faker s_faker = new();
    private AgendaApplicationTestingBuilder _appHost;
    private static readonly JsonSerializerOptions s_jsonSerializerOptions;
    private DistributedApplication _sut;

    static CreateAppointmentEndpointShould()
    {
        s_jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true
        };
        s_jsonSerializerOptions.UseStronglyTypedId();
        s_jsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        s_jsonSerializerOptions.Converters.Add(new MultiFilterConverter());
        s_jsonSerializerOptions.Converters.Add(new FilterConverter());
        s_jsonSerializerOptions.Converters.Add(new PatchJsonConverter());
        s_jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<OperationType>());
        s_jsonSerializerOptions.Converters.Add(new EnumStringConverter<OperationType>());
    }


    ///<inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(outputHelper);

        _sut = await _appHost.StartAsync();
        _client = _appHost.ApiClient;
    }

    ///<inheritdoc/>
    public async ValueTask DisposeAsync() => await _appHost.DisposeAsync();


    [Fact]
    public async Task Returns_the_appointment_when_created_successfully()
    {
        // Arrange
        //_client = _sut.CreateHttpClient("api");
        outputHelper.WriteLine("Client: " + _client.BaseAddress);
        Instant startDate = s_faker.Noda().Instant.Soon();
        Instant endDate = s_faker.Noda().Instant.Future(reference: startDate);

        AppointmentInfo newAppointmentInfo = new ()
        {
            Id = AppointmentId.New(),
            StartDate = startDate.InUtc().ToOffsetDateTime(),
            EndDate = endDate.InUtc().ToOffsetDateTime(),
            Location = s_faker.Address.City(),
            Attendees = s_faker.Make(2, () => new AttendeeInfo { Name = s_faker.Name.FullName(), Email = s_faker.Internet.Email(), PhoneNumber = s_faker.Phone.PhoneNumber() }),
            Subject = s_faker.Lorem.Sentence()
        };

        // Act
        using HttpResponseMessage response = await _client.PostAsJsonAsync("/appointments", newAppointmentInfo, s_jsonSerializerOptions, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        Browsable<AppointmentInfo> browsable = await response.Content.ReadFromJsonAsync<Browsable<AppointmentInfo>>(s_jsonSerializerOptions, cancellationToken: TestContext.Current.CancellationToken);

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