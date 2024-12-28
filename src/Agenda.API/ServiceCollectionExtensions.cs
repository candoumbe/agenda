namespace Agenda.API;

using Agenda.DataStores;

using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;

using Fluxera.StronglyTypedId.SystemTextJson;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NodaTime;
using NodaTime.Serialization.SystemTextJson;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provide extension method used to configure services collection
/// </summary>
public static class ServiceCollectionExtensions
{
    private static Action<JsonSerializerOptions> ConfigureJsonOptions => jsonSerializerOptions =>
    {
        jsonSerializerOptions.UseStronglyTypedId();
        jsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        jsonSerializerOptions.IgnoreReadOnlyFields = true;
        jsonSerializerOptions.IgnoreReadOnlyProperties = true;
        jsonSerializerOptions.AllowTrailingCommas = true;
        jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        jsonSerializerOptions.PropertyNameCaseInsensitive = true;
        jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    };

    /// <summary>
    /// Adds require dependencies for endpoints
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// 
    public static IServiceCollection AddCustomizedMvc(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureHttpJsonOptions(options => ConfigureJsonOptions(options.SerializerOptions));
        services.AddControllers()
            .AddJsonOptions(options => ConfigureJsonOptions(options.JsonSerializerOptions));

        return services;
    }

    /// <summary>
    /// Adds required dependencies to access API datastores
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static IServiceCollection AddDataStores(this IServiceCollection services, IConfiguration configuration)
    {
        using IServiceScope scope = services.BuildServiceProvider().CreateScope();

        services.AddTransient(serviceProvider =>
        {
            DbContextOptionsBuilder<AgendaDataStore> optionsBuilder = BuildDbContextOptions(serviceProvider, configuration);
            IClock clock = serviceProvider.GetRequiredService<IClock>();
            return new AgendaDataStore(optionsBuilder.Options, clock);
        });

        services.AddSingleton<IUnitOfWorkFactory, EntityFrameworkUnitOfWorkFactory<AgendaDataStore>>(serviceProvider =>
        {
            DbContextOptionsBuilder<AgendaDataStore> builder = BuildDbContextOptions(serviceProvider, configuration);

            IClock clock = serviceProvider.GetRequiredService<IClock>();
            return new EntityFrameworkUnitOfWorkFactory<AgendaDataStore>(builder.Options, options => new AgendaDataStore(options, clock), new AgendaRepositoryFactory());
        });

        services.AddAsyncInitializer<DataStoreMigrateInitializerAsync<AgendaDataStore>>();

        return services;

        static DbContextOptionsBuilder<AgendaDataStore> BuildDbContextOptions(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            IHostEnvironment hostingEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            DbContextOptionsBuilder<AgendaDataStore> builder = new();

            string connectionString = configuration.GetConnectionString("Agenda");

            builder.UseNpgsql(connectionString,
                options => options.EnableRetryOnFailure(5).UseNodaTime()
                    .MigrationsAssembly("Agenda.DataStores.Postgres"));
            builder.UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>());
            builder.ConfigureWarnings(options => options.Default(WarningBehavior.Log));
            return builder;
        }
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
        services.Configure<AgendaApiOptions>((options) =>
        {
            options.DefaultPageSize = configuration.GetValue($"ApiOptions:{nameof(AgendaApiOptions.DefaultPageSize)}", 30);
            options.MaxPageSize = configuration.GetValue($"ApiOptions:{nameof(AgendaApiOptions.DefaultPageSize)}", 100);
        });

        services.Configure<JwtOptions>((options) =>
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
    public static IServiceCollection AddCustomizedDependencyInjection(this IServiceCollection services)
    {
        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddHttpContextAccessor();
        services.AddTransient<CurrentRequestMetadataInfoProvider>();

        return services;
    }

    /// <summary>
    /// Adds Swagger middlewares
    /// </summary>
    /// <param name="services"></param>
    /// <param name="hostingEnvironment"></param>
    /// <param name="configuration"></param>
    public static IServiceCollection AddCustomizedSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            string filePath = null;
            if (filePath is not null)
            {
                options.IncludeXmlComments(filePath, true);
            }
        });
        services.AddEndpointsApiExplorer();
        return services;
    }

    ///// <summary>
    ///// Configures the authentication middleware
    ///// </summary>
    ///// <param name="services"></param>
    ///// <param name="configuration"></param>
    //public static IServiceCollection AddCustomAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration configuration)
    //{
    //    services
    //        .AddAuthorization()
    //        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //        .AddJwtBearer(options =>
    //        {
    //            options.TokenValidationParameters = new TokenValidationParameters
    //            {
    //                ValidateIssuer = true,
    //                ValidateAudience = true,
    //                ValidateLifetime = true,
    //                ValidateIssuerSigningKey = true,
    //                ValidIssuer = configuration[$"Authentication:{nameof(JwtOptions)}:{nameof(JwtOptions.Issuer)}"],
    //                ValidAudience = configuration[$"Authentication:{nameof(JwtOptions)}:{nameof(JwtOptions.Audience)}"],
    //                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration[$"Authentication:{nameof(JwtOptions)}:{nameof(JwtOptions.Key)}"])),
    //            };
    //        });

    //    return services;
    //}

    ///// <summary>
    ///// Adds version
    ///// </summary>
    ///// <param name="services"></param>
    ///// <returns></returns>
    //public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
    //{
    //    services.AddApiVersioning(options =>
    //    {
    //        options.AssumeDefaultVersionWhenUnspecified = true;
    //        options.UseApiBehavior = true;
    //        options.ReportApiVersions = true;
    //        options.ApiVersionSelector = new CurrentImplementationApiVersionSelector(options);
    //        options.ApiVersionReader = new HeaderApiVersionReader("api-version");
    //    });
    //    services.AddVersionedApiExplorer(
    //        options =>
    //        {
    //            // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
    //            // note: the specified format code will format the version as "'v'major[.minor][-status]"
    //            options.GroupNameFormat = "'v'VVV";

    //            // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
    //            // can also be used to control the format of the API version in route templates
    //            options.SubstituteApiVersionInUrl = true;
    //            options.AssumeDefaultVersionWhenUnspecified = true;
    //        });

    //    return services;
    //}

    /// <summary>
    /// Adds custom healthcheck
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCustomHealthCheck(this IServiceCollection services)
    {
        services.AddHealthChecks();

        return services;
    }
}