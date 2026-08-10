using Agenda.DataStores;
using Agenda.Migrator;
using Microsoft.EntityFrameworkCore;
using NodaTime;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<MigrationWorker>();
builder.Services.AddSingleton<IClock, SystemClock>(_ => SystemClock.Instance);;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(MigrationWorker.ActivitySourceName));

builder.AddNpgsqlDbContext<AgendaDataStore>("postgres",
                                            configureSettings: options =>
                                            {
                                                options.ConnectionString = $"{builder.Configuration.GetConnectionString("postgres")};GSS Encryption Mode=disable";
                                            },
                                            configureDbContextOptions: optionsBuilder => optionsBuilder.UseNpgsql(o => o.UseNodaTime()
                                                                           .MigrationsAssembly("Agenda.DataStores.Postgres")
                                                                           .ConfigureDataSource(
                                                                               dataSourceBuilder =>
                                                                               {
                                                                                   // Disable GSS encryption mode to avoid issues with Kerberos authentication in some environments
                                                                                   dataSourceBuilder.ConnectionStringBuilder.GssEncryptionMode = Npgsql.GssEncryptionMode.Disable;
                                                                               })
                                                                           ));

IHost host = builder.Build();

await host.RunAsync();