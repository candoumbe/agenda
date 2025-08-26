using System;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using Aspire.Hosting;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Agenda.API.IntegrationTests;

[IntegrationTest]
public class AppHostShould(ITestOutputHelper outputHelper)
{
    private static readonly TimeSpan s_buildStopTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan s_startStopTimeout = TimeSpan.FromSeconds(120);

    [Fact]
    public async Task StartAndStopWithoutException()
    {
        AgendaApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(outputHelper);
        await using DistributedApplication sut = await appHost.StartAsync(s_startStopTimeout).WaitAsync(s_buildStopTimeout);

        await sut.StartAsync().WaitAsync(s_startStopTimeout);
        await sut.WaitForResourcesAsync().WaitAsync(s_startStopTimeout);

        //app.EnsureNoErrorsLogged();

        await sut.StopAsync().WaitAsync(s_buildStopTimeout);
    }
}