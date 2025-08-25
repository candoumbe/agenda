using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Agenda.DataStores;
using Fluxera.StronglyTypedId.SystemTextJson;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Testcontainers.PostgreSql;
using Xunit;

namespace Agenda.API.IntegrationTests.Fixtures;
public class AgendaWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database;
    /// <summary>
    /// Timezone used by default for serialization/deserialization from/to NodaTime types.
    /// </summary>
    public static IDateTimeZoneProvider DefaultDateTimeZone => DateTimeZoneProviders.Tzdb;

    public static Action<JsonSerializerOptions> SerializerOptionsConfigurator => options =>
    {
        options.UseStronglyTypedId();
        options.ConfigureForNodaTime(DefaultDateTimeZone);

        options.Converters.Add(new JsonStringEnumConverter());
        options.IgnoreReadOnlyFields = true;
        options.IgnoreReadOnlyProperties = true;
        options.AllowTrailingCommas = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.PropertyNameCaseInsensitive = true;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    };

    public AgendaWebApplicationFactory()
    {
        _database = new PostgreSqlBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("postgres:17-alpine")
            .WithDatabase("test-database")
            .WithUsername("username")
            .WithPortBinding(5432, true)
            .Build();
    }

    ///<inheritdoc/>
    public async Task InitializeAsync()
    {
        await _database.StartAsync().ConfigureAwait(false);
        // Apply EF Core migrations explicitly now that API no longer auto-migrates
        DbContextOptionsBuilder<AgendaDataStore> optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Agenda.DataStores.AgendaDataStore>();
        optionsBuilder.UseNpgsql(_database.GetConnectionString(), npgsql =>
        {
            npgsql.EnableRetryOnFailure(5);
            npgsql.UseNodaTime();
            npgsql.MigrationsAssembly("Agenda.DataStores.Postgres");
        });
        using AgendaDataStore context = new Agenda.DataStores.AgendaDataStore(optionsBuilder.Options, NodaTime.SystemClock.Instance);
        await context.Database.MigrateAsync().ConfigureAwait(false);
    }

    ///<inheritdoc/>
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
    }

    ///<inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseEnvironment("Development")
            // .UseTestServer()
            .ConfigureLogging(
                loggerBuilder =>
                {
                    loggerBuilder.ClearProviders();
                })
            .ConfigureTestServices(services =>
        {
            services.RemoveAll<AgendaDataStore>();
            IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection([
                        KeyValuePair.Create("ConnectionStrings:Agenda", _database.GetConnectionString())
                    ])
                    .Build();
            services.AddDataStores(configuration);
        });
    }

    ///<inheritdoc/>
    public override async ValueTask DisposeAsync() => await _database.StopAsync().ConfigureAwait(false);

    ///<inheritdoc/>
    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
    }

}