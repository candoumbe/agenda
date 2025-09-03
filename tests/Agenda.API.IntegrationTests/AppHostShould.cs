using System;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using Aspire.Hosting;
using Xunit;
using Xunit.OpenCategories.V3;


namespace Agenda.API.IntegrationTests;

[IntegrationTests]
public class AppHostShould(ITestOutputHelper outputHelper)
{
    private static readonly TimeSpan s_buildStopTimeout = TimeSpan.FromSeconds(120);

    [Fact]
    public async Task StartAndStopWithoutException()
    {
        await using AgendaApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(outputHelper);
        await using DistributedApplication sut = await appHost.StartAsync(TestContext.Current.CancellationToken).WaitAsync(s_buildStopTimeout, TestContext.Current.CancellationToken);

        //app.EnsureNoErrorsLogged();

        await sut.StopAsync(TestContext.Current.CancellationToken).WaitAsync(s_buildStopTimeout, TestContext.Current.CancellationToken);
        // ReSharper disable DisposeOnUsingVariable
        await appHost.DisposeAsync();
        // ReSharper restore DisposeOnUsingVariable
    }
}