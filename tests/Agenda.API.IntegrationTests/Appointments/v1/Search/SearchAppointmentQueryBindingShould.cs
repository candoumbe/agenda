using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using Aspire.Hosting;
using AwesomeAssertions;
using xRetry.v3;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Appointments.v1.Search;

[IntegrationTest]
public sealed class SearchAppointmentQueryBindingShould(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    private const int TransientInfrastructureMaxAttempts = 6;
    private static readonly TimeSpan s_transientInfrastructureRetryDelay = TimeSpan.FromSeconds(3);
    private static readonly HashSet<HttpStatusCode> s_transientInfrastructureStatusCodes = [HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable, HttpStatusCode.BadGateway];

    private HttpClient _client;
    private AgendaApplicationTestingBuilder _appHost;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(outputHelper, TestContext.Current.CancellationToken);
        await _appHost.StartAsync(TestContext.Current.CancellationToken);
        _client = _appHost.ApiClient;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _appHost.DisposeAsync();
    }

    [RetryFact(maxRetries: 3, delayBetweenRetriesMs: 2000, SkipExceptions = [typeof(DistributedApplicationException)])]
    public async Task Return_ok_when_query_contains_iso_offset_datetime_range()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        using HttpResponseMessage response = await ExecuteRequestWithTransientInfrastructureRetryAsync(HttpMethod.Get, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [RetryFact(maxRetries: 3, delayBetweenRetriesMs: 2000, SkipExceptions = [typeof(DistributedApplicationException)])]
    public async Task Return_ok_and_navigation_headers_when_head_query_contains_iso_offset_datetime_range()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        using HttpResponseMessage response = await ExecuteRequestWithTransientInfrastructureRetryAsync(HttpMethod.Head, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().Contain(header => string.Equals(header.Key, "Link", StringComparison.OrdinalIgnoreCase));
        response.Headers.Should().Contain(header => string.Equals(header.Key, "total", StringComparison.OrdinalIgnoreCase));
        response.Headers.Should().Contain(header => string.Equals(header.Key, "count", StringComparison.OrdinalIgnoreCase));

        string total = response.Headers.First(header => string.Equals(header.Key, "total", StringComparison.OrdinalIgnoreCase)).Value.Single();
        string count = response.Headers.First(header => string.Equals(header.Key, "count", StringComparison.OrdinalIgnoreCase)).Value.Single();

        total.Should().NotBeNullOrWhiteSpace();
        count.Should().NotBeNullOrWhiteSpace();
    }

    private async Task<HttpResponseMessage> ExecuteRequestWithTransientInfrastructureRetryAsync(HttpMethod method, CancellationToken cancellationToken)
    {
        Exception lastTransientException = null;
        HttpResponseMessage finalResponse = null;

        for (int attempt = 1; attempt <= TransientInfrastructureMaxAttempts; attempt++)
        {
            try
            {
                using HttpRequestMessage request = new(method, "/appointments?page=1&pageSize=10&from=2026-05-23T22:00:00.000Z&to=2026-06-08T21:59:59.999Z");
                HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (attempt < TransientInfrastructureMaxAttempts && s_transientInfrastructureStatusCodes.Contains(response.StatusCode))
                {
                    outputHelper.WriteLine($"Transient infrastructure response detected on search {method} attempt {attempt}/{TransientInfrastructureMaxAttempts}. Status code: {(int)response.StatusCode}");
                    response.Dispose();
                }
                else
                {
                    finalResponse = response;
                    break;
                }
            }
            catch (HttpRequestException exception) when (attempt < TransientInfrastructureMaxAttempts)
            {
                lastTransientException = exception;
                outputHelper.WriteLine($"Transient HTTP failure detected on search {method} attempt {attempt}/{TransientInfrastructureMaxAttempts}: {exception.Message}");
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested && attempt < TransientInfrastructureMaxAttempts)
            {
                lastTransientException = exception;
                outputHelper.WriteLine($"Transient timeout detected on search {method} attempt {attempt}/{TransientInfrastructureMaxAttempts}: {exception.Message}");
            }

            if (attempt < TransientInfrastructureMaxAttempts)
            {
                await Task.Delay(s_transientInfrastructureRetryDelay, cancellationToken);
            }
        }

        if (finalResponse is null)
        {
            throw new HttpRequestException($"Search {method} request failed after transient infrastructure retries.", lastTransientException);
        }

        return finalResponse;
    }
}