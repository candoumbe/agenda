using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.v1.Appointments;
using Agenda.API.IntegrationTests.Fixtures;
using Agenda.Ids;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using AwesomeAssertions;
using Bogus;
using Candoumbe.Forms;
using DataFilters.Converters;
using Json.More;
using Json.Patch;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using xRetry.v3;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Appointments.v1.Create;

[IntegrationTest]
[Feature(nameof(Appointments))]
public class CreateAppointmentEndpointShould(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    private HttpClient _client;
    private static readonly Faker s_faker = new();
    private AgendaApplicationTestingBuilder _appHost;
    private static readonly JsonSerializerOptions s_jsonSerializerOptions;
    private DistributedApplication _sut;
    private const int s_transientInfrastructureMaxAttempts = 3;
    private static readonly TimeSpan s_transientInfrastructureRetryDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan s_firstRequestTimeout = TimeSpan.FromSeconds(15);
    private static readonly HttpStatusCode[] s_transientInfrastructureStatusCodes = [HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable, HttpStatusCode.BadGateway];

    static CreateAppointmentEndpointShould()
    {
        s_jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true
        };

        s_jsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        s_jsonSerializerOptions.Converters.Add(new MultiFilterConverter());
        s_jsonSerializerOptions.Converters.Add(new FilterConverter());
        s_jsonSerializerOptions.Converters.Add(new PatchJsonConverter());
        s_jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<OperationType>());
        s_jsonSerializerOptions.Converters.Add(new EnumStringConverter<OperationType>());
        s_jsonSerializerOptions.Converters.Add(new AppointmentId.AppointmentIdSystemTextJsonConverter());
        s_jsonSerializerOptions.Converters.Add(new AttendeeId.AttendeeIdSystemTextJsonConverter());
    }


    ///<inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(outputHelper);

        _sut = await _appHost.StartAsync(TestContext.Current.CancellationToken);
        _client = _appHost.ApiClient;
    }

    ///<inheritdoc/>
    public async ValueTask DisposeAsync() => await _appHost.DisposeAsync();


    [RetryFact(maxRetries: 3, delayBetweenRetriesMs: 2000, SkipExceptions = [typeof(DistributedApplicationException)])]
    public async Task Returns_the_appointment_when_created_successfully()
    {
        // Arrange
        //_client = _sut.CreateHttpClient("api");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        outputHelper.WriteLine("Client: " + _client.BaseAddress);
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
        using CancellationTokenSource firstRequestCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        firstRequestCancellationTokenSource.CancelAfter(s_firstRequestTimeout);
        using HttpResponseMessage response = await ExecuteCreateRequestWithTransientInfrastructureRetryAsync(newAppointmentInfo, firstRequestCancellationTokenSource.Token);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.Created);

        Browsable<AppointmentInfo> browsable = await response.Content.ReadFromJsonAsync<Browsable<AppointmentInfo>>(s_jsonSerializerOptions, cancellationToken: cancellationToken);

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

    private async Task<HttpResponseMessage> ExecuteCreateRequestWithTransientInfrastructureRetryAsync(AppointmentInfo newAppointmentInfo, CancellationToken cancellationToken)
    {
        Exception lastTransientException = null;
        HttpResponseMessage finalResponse = null;

        for (int attempt = 1; attempt <= s_transientInfrastructureMaxAttempts; attempt++)
        {
            try
            {
                HttpResponseMessage response = await _client.PostAsJsonAsync("/appointments", newAppointmentInfo, s_jsonSerializerOptions, cancellationToken: cancellationToken);

                bool shouldRetry = await ShouldRetryBecauseOfTransientInfrastructureFailureAsync(response, attempt, cancellationToken);

                if (!shouldRetry)
                {
                    finalResponse = response;
                    break;
                }

                response.Dispose();
            }
            catch (HttpRequestException exception) when (attempt < s_transientInfrastructureMaxAttempts)
            {
                lastTransientException = exception;
                outputHelper.WriteLine($"Transient HTTP failure detected on create attempt {attempt}/{s_transientInfrastructureMaxAttempts}: {exception.Message}");
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested && attempt < s_transientInfrastructureMaxAttempts)
            {
                lastTransientException = exception;
                outputHelper.WriteLine($"Transient timeout detected on create attempt {attempt}/{s_transientInfrastructureMaxAttempts}: {exception.Message}");
            }

            if (attempt < s_transientInfrastructureMaxAttempts)
            {
                await Task.Delay(s_transientInfrastructureRetryDelay, cancellationToken);
            }
        }

        if (finalResponse is null)
        {
            if (lastTransientException is not null)
            {
                throw new HttpRequestException("Create appointment request failed after transient infrastructure retries.", lastTransientException);
            }

            throw new TimeoutException("Create appointment request did not complete successfully before retry timeout elapsed.");
        }

        return finalResponse;
    }

    private async Task<bool> ShouldRetryBecauseOfTransientInfrastructureFailureAsync(HttpResponseMessage response, int attempt, CancellationToken cancellationToken)
    {
        if (attempt < s_transientInfrastructureMaxAttempts && s_transientInfrastructureStatusCodes.Contains(response.StatusCode))
        {
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            outputHelper.WriteLine($"Transient infrastructure response detected on create attempt {attempt}/{s_transientInfrastructureMaxAttempts}. Status code: {(int)response.StatusCode}. Body: {responseContent}");
            return true;
        }

        return false;
    }
}