using System.Globalization;
using System.Security.Claims;
using Agenda.API.Authentication;
using Agenda.DataStores;
using Agenda.DataStores.Postgres;
using Agenda.Events;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;
using Candoumbe.Types.Numerics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Paramore.Brighter;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter.MessagingGateway.RMQ.Async;
using Paramore.Brighter.Observability;
using Paramore.Brighter.Outbox.Hosting;
using Paramore.Brighter.Outbox.PostgreSql;
using Paramore.Brighter.PostgreSql;
using Paramore.Brighter.PostgreSql.EntityFrameworkCore;
using RabbitMQ.Client;

namespace Agenda.API;

/// <summary>
/// Provide extension method used to configure services collection
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string TestingNowConfigKey = "Testing:Now";
    /// <summary>
    /// Valid algorithms for JWT token validation.
    /// </summary>
    private static readonly string[] s_validAlgorithms = ["RS256"];

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds required dependencies to access API datastores.
        /// </summary>
        public void AddDataStores()
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

            services.AddHealthChecks()
                    .AddDbContextCheck<AgendaDataStore>(name: "agenda-datastore-readiness", tags: ["ready"]);
        }

        /// <summary>
        /// Adds supports for Options
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public IServiceCollection AddCustomOptions(IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<AgendaApiOptions>(options =>
            {
                options.PaginationOptions = new PaginationOptions()
                {
                    DefaultPageSize = PositiveInteger.From(configuration.GetValue($"ApiOptions:{nameof(AgendaApiOptions.PaginationOptions.DefaultPageSize)}", 30)),
                    MaxPageSize = PositiveInteger.From(configuration.GetValue($"ApiOptions:{nameof(AgendaApiOptions.PaginationOptions.DefaultPageSize)}", 100))
                };
                options.MessagingOptions = new MessagingOptions() { OutboxTablename = configuration.GetValue<string>($"ApiOptions:{nameof(AgendaApiOptions.MessagingOptions.OutboxTablename)}") };
            });

            return services;
        }

        /// <summary>
        /// Configure dependency injection container
        /// </summary>
        public void AddCustomizedDependencyInjection(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddSingleton<IClock>(_ => ResolveClock(configuration));
            services.AddHttpContextAccessor();
            services.AddTransient<CurrentRequestMetadataInfoProvider>();
        }

        /// <summary>
        /// Wires Keycloak-backed JWT bearer authentication and the default
        /// "must be authenticated" authorization policy.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="environment">Hosting environment used to relax HTTPS metadata in development.</param>
        public IServiceCollection AddCustomAuthentication(IConfiguration configuration, IHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(environment);

            string realm = configuration.GetValue("Authentication:Keycloak:Realm", "agenda");
            string audience = configuration.GetValue("Authentication:Keycloak:Audience", "agenda-api");
            (string authority, bool requireHttpsMetadata) = ResolveKeycloakAuthority(configuration, realm, environment);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddKeycloakJwtBearer("keycloak", realm: realm, configureOptions: opt =>
                    {
                        opt.Audience = audience;

                        if (!string.IsNullOrWhiteSpace(authority))
                        {
                            opt.Authority = authority;
                        }

                        opt.RequireHttpsMetadata = requireHttpsMetadata;
                        opt.TokenValidationParameters.ValidAlgorithms = s_validAlgorithms;
                        opt.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
                        opt.TokenValidationParameters.NameClaimType = "preferred_username";
                        opt.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                    });

            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                                            .RequireAuthenticatedUser()
                                            .Build();
            });

            services.AddTransient<IClaimsTransformation, RealmRolesClaimsTransformation>();

            return services;
        }

        /// <summary>
        /// Adds support for Brighter.
        /// </summary>
        /// <param name="configuration">The configuration to use</param>
        /// <param name="environment">The environment onto which the setup is performed</param>
        public void AddCustomBrighter(IConfiguration configuration, IHostEnvironment environment)
        {
            string databaseConnectionString = configuration.GetConnectionString("postgres")!.WithGssDisabled();
            MessagingOptions messagingOptions = configuration.GetSection($"ApiOptions:{nameof(AgendaApiOptions.MessagingOptions)}").Get<MessagingOptions>();
            bool runningIntegrationTests = configuration.GetValue<bool>("RunningIntegrationTests");
            RelationalDatabaseConfiguration outboxConfiguration = new(databaseConnectionString, outBoxTableName: "outbox");

            services.AddSingleton<IAmARelationalDatabaseConfiguration>(outboxConfiguration);

            IBrighterBuilder brighterBuilder = services.AddBrighter()
                .AddProducers(producers =>
                {
                    RmqMessagingGatewayConnection rmqMessagingGatewayConnection = new()
                    {
                        AmpqUri = new AmqpUriSpecification(new Uri(configuration.GetConnectionString("messaging")!)),
                        PersistMessages = true,
                        Name = $"agenda.{environment.EnvironmentName}.outgoing",
                        Exchange = new Exchange($"agenda.{environment.EnvironmentName}.events", type: ExchangeType.Topic),
                    };
                    List<RmqPublication> publications =
                    [
                        new RmqPublication<AppointmentScheduled>()
                        {
                            Topic = "agenda.appointments.scheduled",
                            MakeChannels = OnMissingChannel.Create,
                            WaitForConfirmsTimeOutInMilliseconds = 1000,
                            Subject = "appointment/scheduled",
                        },
                        new RmqPublication<AppointmentCreated>()
                        {
                            Topic = "agenda.appointments.created",
                            MakeChannels = OnMissingChannel.Create,
                            WaitForConfirmsTimeOutInMilliseconds = 1000,
                            Subject = "appointment/created",
                        }
                    ];
                    producers.ProducerRegistry = new RmqProducerRegistryFactory(rmqMessagingGatewayConnection, publications).Create();
                    producers.Outbox = new PostgreSqlOutbox(outboxConfiguration);
                    producers.ConnectionProvider = typeof(PostgreSqlConnectionProvider);
                    producers.TransactionProvider = typeof(PostgreSqlEntityFrameworkTransactionProvider<AgendaDataStore>);
                });

            if (!runningIntegrationTests)
            {
                brighterBuilder.UseOutboxSweeper();
            }
        }
    }

    /// <summary>
    /// Resolves the concrete Keycloak realm URL advertised by Aspire service discovery.
    /// </summary>
    /// <remarks>
    /// <c>AddKeycloakJwtBearer</c> assigns the composite service discovery scheme
    /// <c>https+http://keycloak/realms/{realm}</c> to the authority. The JWT bearer post-configuration
    /// only accepts a metadata address starting with <c>https://</c>, so it throws on every single request
    /// as soon as HTTPS metadata is required, which turns the whole API into a blanket <c>500</c>.
    /// Resolving the endpoint injected by the AppHost yields a real absolute URL and lets the HTTPS
    /// requirement follow the scheme that is actually in use.
    /// </remarks>
    private static (string Authority, bool RequireHttpsMetadata) ResolveKeycloakAuthority(IConfiguration configuration, string realm, IHostEnvironment environment)
    {
        string baseAddress = configuration["services:keycloak:https:0"] ?? configuration["services:keycloak:http:0"];

        string authority = string.IsNullOrWhiteSpace(baseAddress)
            ? null
            : $"{baseAddress.TrimEnd('/')}/realms/{realm}";

        bool defaultRequireHttpsMetadata = authority is not null
            ? authority.StartsWith(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            : !environment.IsDevelopment();

        return (authority, configuration.GetValue("Authentication:Keycloak:RequireHttpsMetadata", defaultRequireHttpsMetadata));
    }

    private static IClock ResolveClock(IConfiguration configuration)    {
        string configuredNow = configuration.GetValue<string>(TestingNowConfigKey);
        if (string.IsNullOrWhiteSpace(configuredNow))
        {
            return NodaTime.SystemClock.Instance;
        }

        bool parsed = DateTimeOffset.TryParse(configuredNow,
                                              CultureInfo.InvariantCulture,
                                              DateTimeStyles.RoundtripKind,
                                              out DateTimeOffset now);

        return parsed ? new FrozenClock(Instant.FromDateTimeOffset(now)) : NodaTime.SystemClock.Instance;
    }

    private sealed class FrozenClock : IClock
    {
        private readonly Instant _instant;

        public FrozenClock(Instant instant)
        {
            _instant = instant;
        }

        public Instant GetCurrentInstant() => _instant;
    }
}