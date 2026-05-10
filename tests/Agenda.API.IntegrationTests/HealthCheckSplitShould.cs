using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests;

[IntegrationTest]
public class HealthCheckSplitShould
{
    private static readonly TimeSpan s_startStopTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Return_unhealthy_from_health_but_healthy_from_alive_when_readiness_check_fails()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck("failing-ready-check", () => HealthCheckResult.Unhealthy("Simulated readiness failure"), ["ready"]);

        await using WebApplication app = builder.Build();

        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });

        await app.StartAsync(cancellationToken).WaitAsync(s_startStopTimeout, cancellationToken);

        using HttpClient client = app.GetTestClient();

        // Act
        using HttpResponseMessage healthResponse = await client.GetAsync("/health", cancellationToken).WaitAsync(s_startStopTimeout, cancellationToken);
        using HttpResponseMessage aliveResponse = await client.GetAsync("/alive", cancellationToken).WaitAsync(s_startStopTimeout, cancellationToken);

        // Assert
        healthResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        aliveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
