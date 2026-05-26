using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using AwesomeAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;
using Xunit;

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
    private static readonly TimeSpan s_defaultTimeout = 30.Seconds();
#pragma warning disable IDE1006 // Styles d'affectation de noms
    private static int s_httpsCertificateChecked;
#pragma warning restore IDE1006 // Styles d'affectation de noms

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationTestingBuilderFactory"/> class.
    /// </summary>
    /// <param name="outputHelper"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<AgendaApplicationTestingBuilder> CreateBuilderAsync(ITestOutputHelper outputHelper = null, CancellationToken cancellationToken = default)
    {
        EnsureDeveloperHttpsCertificate();
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder.CreateAsync<Agenda_AppHost>(cancellationToken);


        builder.Configuration.AddInMemoryCollection([new KeyValuePair<string, string>("RunningIntegrationTests", bool.TrueString)]);
        builder.Configuration["ConnectionStrings:postgres"] += ";SSL Mode=Disable";
        
        builder.WithRandomParameterValues();
        builder.WithRandomVolumeNames();
        // Session lifetime keeps resources scoped to the current test run
        // and avoids stale containers reused across runs.
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
                                        logging.SetMinimumLevel(LogLevel.Debug);
                                        logging.AddFilter("Aspire", LogLevel.Critical);
                                        logging.AddFilter(builder.Environment.ApplicationName, LogLevel.Information);
                                    });

        return new AgendaApplicationTestingBuilder(builder);
    }

    private static void EnsureDeveloperHttpsCertificate()
    {
        if (Interlocked.Exchange(ref s_httpsCertificateChecked, 1) == 1)
        {
            return;
        }

        ProcessStartInfo checkCertificateStartInfo = new("dotnet", "dev-certs https --check")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process checkCertificateProcess = Process.Start(checkCertificateStartInfo);
        checkCertificateProcess?.WaitForExit();

        if (checkCertificateProcess is not null && checkCertificateProcess.ExitCode == 0)
        {
            return;
        }

        ProcessStartInfo createCertificateStartInfo = new("dotnet", "dev-certs https")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process createCertificateProcess = Process.Start(createCertificateStartInfo);
        createCertificateProcess?.WaitForExit();

        if (createCertificateProcess is null || createCertificateProcess.ExitCode != 0)
        {
            string errorOutput = createCertificateProcess?.StandardError.ReadToEnd() ?? string.Empty;
            string standardOutput = createCertificateProcess?.StandardOutput.ReadToEnd() ?? string.Empty;
            throw new InvalidOperationException($"Unable to create ASP.NET Core developer HTTPS certificate. stdout='{standardOutput}' stderr='{errorOutput}'");
        }
    }
}