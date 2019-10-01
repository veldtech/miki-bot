namespace Miki
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Accounts;
    using Miki.Adapters;
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
    using Miki.Framework.Commands.Localization;
    using Miki.Framework.Commands.Permissions;
    using Miki.Framework.Commands.Permissions.Models;
    using Miki.Framework.Commands.Stages;
    using Miki.Framework.Events;
    using Miki.Framework.Events.Triggers;
    using Miki.Localization;
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;
    using Miki.Logging;
    using Miki.Models.Objects.Backgrounds;
    using Miki.Patterns.Repositories;
    using Miki.Serialization;
    using Miki.Serialization.Protobuf;
    using Miki.Services.Achievements;
    using Miki.Services.Localization;
    using Miki.Services.Rps;
    using Miki.UrbanDictionary;
    using Retsu.Consumer;
    using SharpRaven;
    using SharpRaven.Data;
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
            IAsyncEventingExecutor<IDiscordMessage> pipeline)
        {
            var discordClient = services.GetService<IDiscordClient>();
            discordClient.UserUpdate += Client_UserUpdated;
            discordClient.GuildJoin += Client_JoinedGuild;  

            discordClient.MessageCreate += async (e) => await pipeline.ExecuteAsync(e);
            pipeline.OnExecuted += LogErrors;

            return new ProviderCollection()
                .Add(ProviderAdapter.Factory(
                    discordClient.Gateway.StartAsync,
                    discordClient.Gateway.StopAsync));
        }

        private async ValueTask LogErrors(IExecutionResult<IDiscordMessage> arg)
        {   
            if(!arg.Success)
            {
                if(arg.Error is LocalizedException botEx)
                {
                    await arg.Context.ErrorEmbedResource(botEx.LocaleResource)
                        .ToEmbed()
                        .QueueAsync(arg.Context.GetChannel());
                }
                else
                {
                    Log.Error(arg.Error);
                    var sentry = arg.Context.GetService<RavenClient>();
                    if(sentry != null)
                    {
                        await sentry.CaptureAsync(new SentryEvent(arg.Error));
                    }
                }
            }
        }

        public override IAsyncEventingExecutor<IDiscordMessage> ConfigurePipeline(IServiceProvider services)
        {
            LoadLocales(services); // TODO(velddev): Find a better place to load this.

            var commandTree = new CommandTreeBuilder(services)
                .AddCommandBuildStep(new ConfigurationManagerAdapter())
                .Create(Assembly.GetExecutingAssembly());

            return new CommandPipelineBuilder(services)
                .UseStage(new CorePipelineStage())
                .UseFilters(
                    new BotFilter(),
                    new UserFilter())
                .UsePrefixes(
                    new PrefixTrigger(">", true, true),
                    new PrefixTrigger("miki.", false),
                    new MentionTrigger())
                .UseStage(new FetchDataStage())
                .UseLocalization()
                .UseArgumentPack()
                .UseCommandHandler(commandTree)
                .UsePermissions()
                .UseScopes()
                .Build();
        }

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
                // TODO(velddev): Remove constant environment fetch.
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
            serviceCollection.AddSingleton<
                IAsyncRepository<Permission>, EntityRepository<Permission>>();

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
                serviceCollection.AddSingleton<ILocalizationService, LocalizationService>();
                serviceCollection.AddSingleton<PermissionService>();
            }

        }

        private static void LoadLocales(IServiceProvider services)
        {
            var localizationService = services.GetRequiredService<ILocalizationService>();
            
            const string nameSpace = "Miki.Languages";
            var typeList = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && t.Namespace == nameSpace);

            foreach(var t in typeList)
            {
                try
                {
                    string languageName = t.Name.ToLowerInvariant();

                    ResourceManager resources = new ResourceManager(
                        $"Miki.Languages.{languageName}",
                        t.Assembly);
                    
                    IResourceManager resourceManager = new ResxResourceManager(resources);

                    localizationService.AddLocale(new Locale(languageName, resourceManager));
                }
                catch(Exception ex)
                {
                    Log.Error($"Language {t.Name} did not load correctly");
                    Log.Debug(ex.ToString());
                }
            }
        }

        private async Task Client_UserUpdated(IDiscordUser oldUser, IDiscordUser newUser)
        {
            using var scope = Services.CreateScope();
            if(oldUser.AvatarId != newUser.AvatarId)
            {
                await Utils.SyncAvatarAsync(newUser,
                        scope.ServiceProvider.GetService<IExtendedCacheClient>(),
                        scope.ServiceProvider.GetService<MikiDbContext>())
                    .ConfigureAwait(false);
            }
        }

        private async Task Client_JoinedGuild(IDiscordGuild arg)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetService<DbContext>();

            IDiscordChannel defaultChannel = await arg.GetDefaultChannelAsync()
                .ConfigureAwait(false);
            if(defaultChannel != null)
            {
                var locale = scope.ServiceProvider.GetService<ILocalizationService>();
                Locale i = await locale.GetLocaleAsync((long)defaultChannel.Id)
                    .ConfigureAwait(false);
                (defaultChannel as IDiscordTextChannel).QueueMessage(i.GetString("miki_join_message"));
            }

            List<string> allArgs = new List<string>();
            List<object> allParams = new List<object>();
            List<object> allExpParams = new List<object>();

            try
            {
                var members = await arg.GetMembersAsync();
                for(int i = 0; i < members.Count(); i++)
                {
                    allArgs.Add($"(@p{i * 2}, @p{i * 2 + 1})");

                    allParams.Add(members.ElementAt(i).Id.ToDbLong());
                    allParams.Add(members.ElementAt(i).Username);

                    allExpParams.Add(arg.Id.ToDbLong());
                    allExpParams.Add(members.ElementAt(i).Id.ToDbLong());
                }

                await context.Database.ExecuteSqlCommandAsync(
                    $"INSERT INTO dbo.\"Users\" (\"Id\", \"Name\") VALUES {string.Join(",", allArgs)} ON CONFLICT DO NOTHING",
                    allParams);

                await context.Database.ExecuteSqlCommandAsync(
                    $"INSERT INTO dbo.\"LocalExperience\" (\"ServerId\", \"UserId\") VALUES {string.Join(",", allArgs)} ON CONFLICT DO NOTHING",
                    allExpParams);

                await context.SaveChangesAsync();
            }
            catch(Exception e)
            {
                Log.Error(e.ToString());
            }
        }

    }
}
