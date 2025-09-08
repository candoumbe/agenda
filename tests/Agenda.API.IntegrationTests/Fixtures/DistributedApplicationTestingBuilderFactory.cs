using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;
using Xunit.Abstractions;

namespace Agenda.API.IntegrationTests.Fixtures;

/// <summary>
/// Factory for creating <see cref="AgendaApplicationTestingBuilder"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This class is used to create a new instance of the <see cref="AgendaApplicationTestingBuilder"/> class for each test.
/// </para>
/// <para>
/// This is required because the <see cref="AgendaApplicationTestingBuilder"/> class is not thread safe and each test should use its own instance.
/// </para>
/// For more informations, <see href="https://github.com/dotnet/aspire-samples/blob/main/tests/SamplesIntegrationTests/Infrastructure/DistributedApplicationTestFactory.cs">the GitHub sample</see>.
/// </remarks>
public static class DistributedApplicationTestingBuilderFactory
{
    private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationTestingBuilderFactory"/> class.
    /// </summary>
    /// <param name="outputHelper"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<AgendaApplicationTestingBuilder> CreateBuilderAsync(ITestOutputHelper outputHelper = null, CancellationToken cancellationToken = default)
    {
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder.CreateAsync<Agenda_AppHost>(cancellationToken);

        builder.WithRandomParameterValues();
        builder.WithRandomVolumeNames();
        // Containers should be re-created for each test.
        builder.WithContainersLifetime(ContainerLifetime.Session);

        builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
                                                     {
                                                         clientBuilder.AddStandardResilienceHandler();
                                                     });

        builder.Services.AddHttpLogging();
        builder.Services.AddLogging(logging =>
                                    {
                                        logging.ClearProviders();
                                        logging.AddSimpleConsole();
                                        if (outputHelper is not null)
                                        {
                                            logging.AddXUnit(outputHelper);
                                        }
                                        logging.SetMinimumLevel(LogLevel.Information);
                                        logging.AddFilter("Aspire", LogLevel.Critical);
                                        logging.AddFilter(builder.Environment.ApplicationName, LogLevel.Information);
                                    });

        return new AgendaApplicationTestingBuilder(builder);
    }
}