using Agenda.DataStores;
using Agenda.Migrator;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Microsoft.EntityFrameworkCore;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<MigrationWorker>();
builder.Services.AddSingleton<IClock, SystemClock>(_ => SystemClock.Instance);;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(MigrationWorker.ActivitySourceName));

builder.AddNpgsqlDbContext<AgendaDataStore>("postgres",
                                            configureDbContextOptions: optionsBuilder => optionsBuilder.UseNpgsql(o => o.UseNodaTime()
                                                                           .MigrationsAssembly("Agenda.DataStores.Postgres")));

IHost host = builder.Build();

await host.RunAsync();