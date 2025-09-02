using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Agenda.DataStores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Agenda.Migrator;

/// <summary>
/// Worker that runs migrations.
/// </summary>
public class MigrationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    /// <summary>
    /// Worker that runs migrations.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> used to resolve worker's dependencies.</param>
    /// <param name="hostApplicationLifetime">Hook into the application's host's lifetime</param>
    /// <exception cref="ArgumentNullException">if <paramref name="serviceProvider"/> or <paramref name="hostApplicationLifetime"/> is <c>null</c>.</exception>
    public MigrationWorker(IServiceProvider serviceProvider,
                           IHostApplicationLifetime hostApplicationLifetime)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
    }

    internal const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);


    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = s_activitySource.StartActivity();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            AgendaDataStore dbContext = scope.ServiceProvider.GetRequiredService<AgendaDataStore>();

            await RunMigrationAsync(dbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        _hostApplicationLifetime.StopApplication();

        return;

        static async Task RunMigrationAsync(DbContext dbContext, CancellationToken cancellationToken)
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
                                        {
                                            // Run migration in a transaction to avoid partial migration if it fails.
                                            await dbContext.Database.MigrateAsync(cancellationToken);
                                        });
        }
    }


}