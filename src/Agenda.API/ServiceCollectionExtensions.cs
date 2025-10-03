using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agenda.DataStores;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Agenda.API;

/// <summary>
/// Provide extension method used to configure services collection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds required dependencies to access API datastores
    /// </summary>
    /// <param name="services"></param>
    public static void AddDataStores(this IServiceCollection services)
    {
        using IServiceScope scope = services.BuildServiceProvider().CreateScope();

        services.AddTransient(serviceProvider =>
        {
            DbContextOptions<AgendaDataStore> dbContextOptions = serviceProvider.GetRequiredService<DbContextOptions<AgendaDataStore>>();
            IClock clock = serviceProvider.GetRequiredService<IClock>();
            return new AgendaDataStore(dbContextOptions, clock);
        });

        services.AddSingleton<IUnitOfWorkFactory, EntityFrameworkUnitOfWorkFactory<AgendaDataStore>>(serviceProvider =>
        {
            DbContextOptions<AgendaDataStore> dbContextOptions = serviceProvider.GetRequiredService<DbContextOptions<AgendaDataStore>>();

            IClock clock = serviceProvider.GetRequiredService<IClock>();
            return new EntityFrameworkUnitOfWorkFactory<AgendaDataStore>(dbContextOptions, options => new AgendaDataStore(options, clock), new AgendaRepositoryFactory());
        });

        return;

    }

    /// <summary>
    /// Adds supports for Options
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();
        services.Configure<AgendaApiOptions>(options =>
        {
            options.DefaultPageSize = configuration.GetValue($"ApiOptions:{nameof(AgendaApiOptions.DefaultPageSize)}", 30);
            options.MaxPageSize = configuration.GetValue($"ApiOptions:{nameof(AgendaApiOptions.DefaultPageSize)}", 100);
        });

        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = configuration.GetValue<string>($"Authentication:{nameof(JwtOptions)}:{nameof(JwtOptions.Issuer)}");
            options.Audience = configuration.GetValue<string>($"Authentication:{nameof(JwtOptions)}:{nameof(JwtOptions.Audience)}");
            options.Key = configuration.GetValue<string>($"Authentication:{nameof(JwtOptions)}:{nameof(JwtOptions.Key)}");
        });

        return services;
    }

    /// <summary>
    /// Configure dependency injection container
    /// </summary>
    /// <param name="services"></param>
    /// <remarks>
    /// Adds the
    /// </remarks>
    public static void AddCustomizedDependencyInjection(this IServiceCollection services)
    {
        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddHttpContextAccessor();
        services.AddTransient<CurrentRequestMetadataInfoProvider>();
    }
}