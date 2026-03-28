using Agenda.DataStores;
using Agenda.Events;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;
using Candoumbe.Types.Numerics;
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
                    .AddDbContextCheck<AgendaDataStore>(tags: ["ready"]);
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
        public void AddCustomizedDependencyInjection()
        {
            services.AddSingleton<IClock>(SystemClock.Instance);
            services.AddHttpContextAccessor();
            services.AddTransient<CurrentRequestMetadataInfoProvider>();
        }

        /// <summary>
        /// Adds support for Brighter.
        /// </summary>
        /// <param name="configuration">The configuration to use</param>
        /// <param name="environment">The environment onto which the setup is performed</param>
        public void AddCustomBrighter(IConfiguration configuration, IHostEnvironment environment)
        {
            string databaseConnectionString = configuration.GetConnectionString("postgres")!;
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
}