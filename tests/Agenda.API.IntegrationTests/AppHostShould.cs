using System;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using Aspire.Hosting;
using xRetry.v3;
using Xunit;
using Xunit.OpenCategories.V3;


namespace Agenda.API.IntegrationTests;

[IntegrationTest]
public class AppHostShould(ITestOutputHelper outputHelper)
{
    private static readonly TimeSpan s_buildStopTimeout = TimeSpan.FromSeconds(120);

    [RetryFact(maxRetries: 3, delayBetweenRetriesMs: 2000, SkipExceptions = [typeof(DistributedApplicationException)])]
    public async Task Start_and_stop_without_exception()
    {
        await using AgendaApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(outputHelper, TestContext.Current.CancellationToken);
        await using DistributedApplication sut = await appHost.StartAsync(TestContext.Current.CancellationToken).WaitAsync(s_buildStopTimeout, TestContext.Current.CancellationToken);

        //app.EnsureNoErrorsLogged();

        await sut.StopAsync(TestContext.Current.CancellationToken).WaitAsync(s_buildStopTimeout, TestContext.Current.CancellationToken);
        // ReSharper disable DisposeOnUsingVariable
        await appHost.DisposeAsync();
        // ReSharper restore DisposeOnUsingVariable
    }
}