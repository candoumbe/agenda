using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Projects;
using Xunit;
using Xunit.Abstractions;

namespace Agenda.API.IntegrationTests.Fixtures;
public class DistributedApplicationTestingBuilderFactory
{
    private static readonly TimeSpan DefaultTimeOut = TimeSpan.FromSeconds(30);

    /// <summary>
    /// HTTP client for the API.
    /// </summary>
    public HttpClient ApiClient { get; private set; }
    public const string ApiResourceName = "api";

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationTestingBuilderFactory"/> class.
    /// </summary>
    /// <param name="outputHelper"></param>
    /// <returns></returns>
    public static async Task<AgendaApplicationTestingBuilder> CreateBuilderAsync(ITestOutputHelper outputHelper = null)
    {
        CancellationToken cancellationToken = new CancellationTokenSource(DefaultTimeOut).Token;
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder.CreateAsync<Agenda_AppHost>(cancellationToken);

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
                                        logging.SetMinimumLevel(LogLevel.Trace);
                                        logging.AddFilter("Aspire", LogLevel.Trace);
                                        logging.AddFilter(builder.Environment.ApplicationName, LogLevel.Trace);
                                    });


        return new AgendaApplicationTestingBuilder(builder);
    }
}