using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Agenda.API.Features;
using Agenda.Ids;
using Aspire.Hosting;
using DataFilters.Converters;
using Json.More;
using Json.Patch;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Xunit;

namespace Agenda.API.IntegrationTests.Fixtures;

public sealed class AgendaApplicationFixture : IAsyncLifetime
{
    private AgendaApplicationTestingBuilder _appHost;

    public HttpClient ApiClient { get; private set; }

    public HttpClient AnonymousApiClient { get; private set; }

    public HttpClient KeycloakClient { get; private set; }

    public JsonSerializerOptions ApiJsonSerializerOptions { get; }

    public AgendaApplicationFixture()
    {
        ApiJsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true
        };

        ApiJsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        ApiJsonSerializerOptions.Converters.Add(new MultiFilterConverter());
        ApiJsonSerializerOptions.Converters.Add(new FilterConverter());
        ApiJsonSerializerOptions.Converters.Add(new PatchJsonConverter());
        ApiJsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<OperationType>());
        ApiJsonSerializerOptions.Converters.Add(new EnumStringConverter<OperationType>());
        ApiJsonSerializerOptions.Converters.Add(new AppointmentId.AppointmentIdSystemTextJsonConverter());
        ApiJsonSerializerOptions.Converters.Add(new AttendeeId.AttendeeIdSystemTextJsonConverter());
    }

    ///<inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(cancellationToken: TestContext.Current.CancellationToken);
        DistributedApplication app = await _appHost.StartAsync(TestContext.Current.CancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync(AgendaApplicationTestingBuilder.ApiResourceName, TestContext.Current.CancellationToken).WaitAsync(AgendaApplicationTestingBuilder.StartStopTimeout, TestContext.Current.CancellationToken);
        ApiClient = _appHost.ApiClient;
        AnonymousApiClient = _appHost.AnonymousApiClient;
        KeycloakClient = _appHost.KeycloakClient;
    }

    /// <summary>
    /// Issues a real Keycloak access token via the Resource Owner Password Grant
    /// against the <c>agenda-frontend</c> client.
    /// </summary>
    public Task<string> IssueAccessTokenAsync(string username, string password, CancellationToken cancellationToken)
        => _appHost.IssueAccessTokenAsync(username, password, cancellationToken);

    ///<inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_appHost is not null)
        {
            await _appHost.DisposeAsync();
        }
    }
}