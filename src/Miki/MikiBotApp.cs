using System.Collections.Generic;
using System.Reflection;

namespace Miki
{
    using System;
    using Amazon.S3;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Accounts;
    using Miki.API;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Repositories;
    using Miki.BunnyCDN;
    using Miki.Cache;
    using Miki.Cache.StackExchange;
    using Miki.Configuration;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Gateway;
    using Miki.Discord.Rest;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Filters;
    using Miki.Framework.Commands.Filters.Filters;
    using Miki.Framework.Events;
    using Miki.Framework.Events.Triggers;
    using Miki.Logging;
    using Miki.Models.Objects.Backgrounds;
    using Miki.Serialization;
    using Miki.Serialization.Protobuf;
    using Miki.Services.Achievements;
    using Miki.Services.Rps;
    using Miki.UrbanDictionary;
    using Retsu.Consumer;
    using SharpRaven;
    using StackExchange.Redis;

    public class MikiBotApp : MikiApp
    {
        private Config Config { get; }

        public MikiBotApp(Config config)
        {
            this.Config = config;
        }

        public override ProviderCollection ConfigureProviders(
            IServiceProvider services, 
            IAsyncExecutor<IDiscordMessage> pipeline)
        {
            var discordClient = services.GetService<IDiscordClient>();
            
            discordClient.MessageCreate += pipeline.ExecuteAsync;

            return new ProviderCollection()
                .Add(ProviderAdapter.Factory(
                    discordClient.Gateway.StartAsync,
                    discordClient.Gateway.StopAsync));
        }

        public override IAsyncExecutor<IDiscordMessage> ConfigurePipeline(IServiceProvider collection)
            => new CommandPipelineBuilder(collection)
                .UseStage(new CorePipelineStage())
                .UseFilters(
                    new BotFilter(),
                    new UserFilter())
                .UsePrefixes(
                    new PrefixTrigger(">", true, true),
                    new PrefixTrigger("miki.", false),
                    new MentionTrigger())
                .UseLocalization()
                .UseArgumentPack()
                .UseCommandHandler(
                    new CommandTreeBuilder(collection).Create(Assembly.GetExecutingAssembly()))
                .UsePermissions()   
                .UseScopes()
                .Build();

        public override void Configure(ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(Config);
            serviceCollection.AddSingleton<ISerializer, ProtobufSerializer>();
            serviceCollection.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(Config.RedisConnectionString));

            // Setup Redis
            serviceCollection.AddSingleton<ICacheClient, StackExchangeCacheClient>();
            serviceCollection.AddSingleton<IExtendedCacheClient, StackExchangeCacheClient>();

            // Setup Entity Framework
            {
                var connString = Environment.GetEnvironmentVariable(Constants.ENV_ConStr);
                if(connString == null)
                {
                    throw new InvalidOperationException("Connection string cannot be null");
                }

                serviceCollection.AddDbContext<MikiDbContext>(
                    x => x.UseNpgsql(connString, b => b.MigrationsAssembly("Miki.Bot.Models")));
                serviceCollection.AddDbContext<DbContext, MikiDbContext>(
                    x => x.UseNpgsql(connString, b => b.MigrationsAssembly("Miki.Bot.Models")));
            }

            serviceCollection.AddScoped<AchievementRepository>();

            // Setup Miki API
            {
                if(!string.IsNullOrWhiteSpace(Config.MikiApiBaseUrl)
                    && !string.IsNullOrWhiteSpace(Config.MikiApiKey))
                {
                    serviceCollection.AddSingleton(new MikiApiClient(Config.MikiApiKey));
                }
                else
                {
                    Log.Warning("No Miki API parameters were supplied, ignoring Miki API.");
                }
            }

            // Setup Amazon CDN Client
            {
                if(!string.IsNullOrWhiteSpace(Config.CdnAccessKey)
                   && !string.IsNullOrWhiteSpace(Config.CdnSecretKey)
                   && !string.IsNullOrWhiteSpace(Config.CdnRegionEndpoint))
                {
                    serviceCollection.AddSingleton(new AmazonS3Client(
                        Config.CdnAccessKey,
                        Config.CdnSecretKey,
                        new AmazonS3Config()
                        {
                            ServiceURL = Config.CdnRegionEndpoint
                        }));
                }
            }

            // Setup Discord
            {
                serviceCollection.AddSingleton<IApiClient>(
                    s => new DiscordApiClient(Config.Token, s.GetService<ICacheClient>()));

                IGateway gateway = null;
                if(bool.Parse(Environment.GetEnvironmentVariable(Constants.ENV_SelfHost).ToLowerInvariant()))
                {
                    gateway = new GatewayCluster(new GatewayProperties
                    {
                        ShardCount = 1,
                        ShardId = 0,
                        Token = Config.Token,
                        Compressed = true,
                        AllowNonDispatchEvents = true
                    });
                }
                else
                {
                    gateway = new RetsuConsumer(new ConsumerConfiguration
                    {
                        ConnectionString = new Uri(Config.RabbitUrl),
                        QueueName = "gateway",
                        ExchangeName = "consumer",
                        ConsumerAutoAck = false,
                        PrefetchCount = 25,
                    });
                }
                serviceCollection.AddSingleton(gateway);

                serviceCollection.AddSingleton<IDiscordClient, DiscordClient>();
            }

            // Setup web services
            serviceCollection.AddSingleton(new UrbanDictionaryAPI());
            serviceCollection.AddSingleton(new BunnyCDNClient(Config.BunnyCdnKey));

            // Setup miscellanious services
            {
                serviceCollection.AddSingleton<ConfigurationManager>();
                serviceCollection.AddSingleton<BackgroundStore>();

                if(!string.IsNullOrWhiteSpace(Config.SharpRavenKey))
                {
                    serviceCollection.AddSingleton(new RavenClient(Config.SharpRavenKey));
                }
                else
                {
                    Log.Warning("Sentry.io key not provided, ignoring distributed error logging...");
                }

                serviceCollection.AddSingleton<AccountService>();
                serviceCollection.AddSingleton<AchievementService>();
                serviceCollection.AddSingleton<RpsService>();
            }

        }
    }
}
