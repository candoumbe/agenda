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
        const int maxRetries = 5;
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            try
            {
                await using AgendaApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilderFactory.CreateBuilderAsync(outputHelper, TestContext.Current.CancellationToken);
                await using DistributedApplication sut = await appHost.StartAsync(TestContext.Current.CancellationToken).WaitAsync(s_buildStopTimeout, TestContext.Current.CancellationToken);

                //app.EnsureNoErrorsLogged();

                await sut.StopAsync(TestContext.Current.CancellationToken).WaitAsync(s_buildStopTimeout, TestContext.Current.CancellationToken);
                // ReSharper disable DisposeOnUsingVariable
                await appHost.DisposeAsync();
                // ReSharper restore DisposeOnUsingVariable

                // Success, exit loop
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;
                if (attempt < maxRetries)
                {
                    await Task.Delay(1000); // Wait 1 second before retrying
                }
            }
        }

        // If we reach here, all attempts failed
        throw new AggregateException($"Test failed after {maxRetries} attempts due to transient errors.", lastException!);
    }
}