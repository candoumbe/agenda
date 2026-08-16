using System.Net.Http;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using Aspire.Hosting;
using AwesomeAssertions;
using Xunit;
using Xunit.OpenCategories.V3;


namespace Agenda.API.IntegrationTests;

[IntegrationTest]
public class AppHostShould(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task Should_start_and_stop_with_no_errors()
    {
        // This test relies on the fact that the fixture will start the application host before any tests are run and stop it after all tests are completed. If there were issues during startup or shutdown, they would likely cause other tests to fail, so we can consider this as a smoke test for the application host lifecycle.
        await using AgendaApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(outputHelper, TestContext.Current.CancellationToken);
        await using DistributedApplication sut = await appHost.StartAsync(TestContext.Current.CancellationToken).WaitAsync(AgendaApplicationTestingBuilder.StartStopTimeout, TestContext.Current.CancellationToken);

        //app.EnsureNoErrorsLogged();

        await sut.StopAsync(TestContext.Current.CancellationToken).WaitAsync(AgendaApplicationTestingBuilder.StartStopTimeout, TestContext.Current.CancellationToken);
        // ReSharper disable DisposeOnUsingVariable
        await appHost.DisposeAsync();
    }
}