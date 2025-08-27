using System;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Agenda.API.IntegrationTests.Fixtures;

public class AgendaApplicationTestingBuilder : IAsyncDisposable
{
    private IDistributedApplicationTestingBuilder _sutBuilder;
    private DistributedApplication _app;

    /// <summary>
    /// Creates a new instance of the <see cref="AgendaApplicationTestingBuilder"/> class.
    /// </summary>
    /// <param name="builder">The builder that will be used to create the infrastructure of the application under test.</param>
    public AgendaApplicationTestingBuilder(IDistributedApplicationTestingBuilder builder)
    {
        _sutBuilder = builder;
    }

    /// <summary>
    /// Builds the infrastructure and starts the application under test.
    /// </summary>
    /// <param name="startStopTimeout">Time to wait after which the application under test will be considered as "not started".</param>
    /// <param name="buildStopTimeOut">Time to wait after which building the infrastructure will be considered as failed.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The application under test</returns>
    /// <remarks>
    /// The application under test is started after the infrastructure is built.
    /// This method will wait for the application to reach the "running" state (i.e. all resources are running or have exited with a success code).
    /// </remarks>
    public async Task<DistributedApplication> StartAsync(TimeSpan startStopTimeout, TimeSpan buildStopTimeOut, CancellationToken cancellationToken = default)
    {
        _app  = await _sutBuilder.BuildAsync(cancellationToken).WaitAsync(buildStopTimeOut, cancellationToken);


        await _app.StartAsync(cancellationToken).WaitAsync(startStopTimeout, cancellationToken);
        await _app.WaitForResourcesAsync(cancellationToken: cancellationToken).WaitAsync(startStopTimeout, cancellationToken);

        return _app;
    }


    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await _sutBuilder.DisposeAsync();
}