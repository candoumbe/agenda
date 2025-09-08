using System;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using Aspire.Hosting;
using Humanizer;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Agenda.API.IntegrationTests;

[IntegrationTest]
public class AppHostShould(ITestOutputHelper outputHelper)
{
    private static readonly TimeSpan s_buildStopTimeout = 120.Seconds();
    private static readonly TimeSpan s_buildStartTimeout = 120.Seconds();

    [Fact]
    public async Task StartAndStopWithoutException()
    {
        await using AgendaApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(outputHelper);
        await using DistributedApplication sut = await appHost.StartAsync().WaitAsync(s_buildStopTimeout);

        //app.EnsureNoErrorsLogged();

        await sut.StopAsync().WaitAsync(s_buildStopTimeout);
        // ReSharper disable DisposeOnUsingVariable
        await appHost.DisposeAsync();
        // ReSharper restore DisposeOnUsingVariable
    }
}