namespace Miki
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Discord.Internal;
    using Framework.Commands.Localization.Services;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Accounts;
    using Miki.Adapters;
    using Miki.API;
    using Miki.API.Backgrounds;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Repositories;
    using Miki.BunnyCDN;
    using Miki.Cache;
    using Miki.Cache.StackExchange;
    using Miki.Configuration;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Common.Packets;
    using Miki.Discord.Common.Packets.API;
    using Miki.Discord.Gateway;
    using Miki.Discord.Rest;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Filters;
    using Miki.Framework.Commands.Localization;
    using Miki.Framework.Commands.Permissions;
    using Miki.Framework.Commands.Prefixes;
    using Miki.Framework.Commands.Prefixes.Triggers;
    using Miki.Framework.Commands.Scopes;
    using Miki.Framework.Commands.Stages;
    using Miki.Localization;
    using Miki.Localization.Exceptions;
    using Miki.Logging;
    using Miki.Modules.Accounts.Services;
    using Miki.Serialization;
    using Miki.Serialization.Protobuf;
    using Miki.Services;
    using Miki.Services.Daily;
    using Miki.Services.Achievements;
    using Miki.Services.Rps;
    using Miki.Services.Transactions;
    using Miki.UrbanDictionary;
    using Miki.Utility;
    using Retsu.Consumer;
    using Sentry;
    using StackExchange.Redis;
    using Veld.Osu;
    using Veld.Osu.V1;
    using System.Text.Json;
    using Miki.Cache.InMemory;
    using Miki.Modules.Internal.Routines;
    using Miki.Services.Lottery;
    using Miki.Services.Pasta;
    using Miki.Services.Scheduling;
    using Splitio.Services.Client.Classes;
    using Miki.Services.Settings;

    public class MikiBotApp : MikiApp
    {
        public override ProviderCollection ConfigureProviders(
            IServiceProvider services,
            IAsyncEventingExecutor<IDiscordMessage> pipeline)
        {
            DatadogRoutine routine = new DatadogRoutine(
                services.GetService<AccountService>(),
                Pipeline,
                services.GetService<Config>(),
                services.GetService<IDiscordClient>());

            var discordClient = services.GetService<IDiscordClient>();
            discordClient.UserUpdate += Client_UserUpdated;
            discordClient.GuildJoin += Client_JoinedGuild;  

            discordClient.MessageCreate += async (e) => await pipeline.ExecuteAsync(e);
            pipeline.OnExecuted += LogErrorsAsync;

            return new ProviderCollection()
                .Add(new ProviderAdapter(
                    discordClient.Gateway.StartAsync,
                    discordClient.Gateway.StopAsync));
        }

        private async ValueTask LogErrorsAsync(IExecutionResult<IDiscordMessage> arg)
        {
            if(arg.Success)
            {
                return;
            }

            if(arg.Error.GetRootException() is LocalizedException botEx)
            {
                await arg.Context.ErrorEmbedResource(botEx.LocaleResource)
                    .ToEmbed()
                    .QueueAsync(arg.Context, arg.Context.GetChannel());
                return;
            }

            Log.Error(arg.Error);
            var sentry = arg.Context.GetService<ISentryClient>();
            if(sentry == null)
            {
                Log.Warning("Sentry was not set up, discarding error log.");
                return;
            }

            sentry.CaptureEvent(arg.Context.ToSentryEvent(arg.Error));
        }

        public override IAsyncEventingExecutor<IDiscordMessage> ConfigurePipeline(
            IServiceProvider services)
        {
            return new CommandPipelineBuilder(services)
                .UseStage(new CorePipelineStage())
                .UseFilters(new BotFilter(), new UserFilter())
                .UsePrefixes()
                .UseStage(new FetchDataStage())
                .UseLocalization()
                .UseArgumentPack()
                .UseCommandHandler()
                .UsePermissions()
                .UseScopes()
                .Build();
        }

        public override async Task ConfigureAsync(ServiceCollection serviceCollection)
        {
            CreateLogger();

            // TODO(velddev): Remove constant environment fetch.
            var connString = Environment.GetEnvironmentVariable(Constants.EnvConStr);
            if(connString == null)
            {
                throw new InvalidOperationException("Connection string cannot be null");
            }

            await using var context = new MikiDbContextFactory().CreateDbContext();
            var config = await new ConfigService(new UnitOfWork(context)).GetOrCreateAnyAsync(null);

            serviceCollection.AddDbContext<MikiDbContext>(
                x => x.UseNpgsql(connString, b => b.MigrationsAssembly("Miki.Bot.Models"))
                    .EnableDetailedErrors());
            serviceCollection.AddDbContext<DbContext, MikiDbContext>(
                x => x.UseNpgsql(connString, b => b.MigrationsAssembly("Miki.Bot.Models"))
                    .EnableDetailedErrors());

            serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();

            serviceCollection.AddSingleton(config);

            serviceCollection.AddSingleton<ISerializer, ProtobufSerializer>();
            
            serviceCollection.AddScoped<
                IRepositoryFactory<Achievement>, AchievementRepository.Factory>();

            serviceCollection.AddScoped(x => new MikiApiClient(x.GetService<Config>().MikiApiKey));

            // Setup Amazon CDN Client

            if(string.IsNullOrWhiteSpace(config.CdnAccessKey)
               || string.IsNullOrWhiteSpace(config.CdnRegionEndpoint))
            {
                serviceCollection.AddSingleton(new AmazonS3Client(
                    new AmazonS3Config
                    {
                        ServiceURL = "https://cdn.miki.ai"
                    }));
            }
            else
            {
                serviceCollection.AddSingleton(new AmazonS3Client(
                    config.CdnAccessKey,
                    config.CdnSecretKey,
                    new AmazonS3Config
                    {
                        ServiceURL = config.CdnRegionEndpoint
                    }));
            }

            // Setup Discord
            serviceCollection.AddSingleton<IApiClient>(
                s => new DiscordApiClient(
                    s.GetService<Config>().Token,
                    s.GetService<ICacheClient>()));

            bool.TryParse(Environment.GetEnvironmentVariable(Constants.EnvSelfHost), out var selfHost);
            if(selfHost)
            {
                serviceCollection.AddSingleton<IGateway>(
                    new GatewayCluster(
                        new GatewayProperties
                        {
                            ShardCount = 1,
                            ShardId = 0,
                            Token = config.Token,
                            AllowNonDispatchEvents = true
                        }));    

                serviceCollection.AddSingleton<ICacheClient, InMemoryCacheClient>();
                serviceCollection.AddSingleton<IExtendedCacheClient, InMemoryCacheClient>();

                var splitConfig = new Splitio.Services.Client.Classes.ConfigurationOptions
                {
                    LocalhostFilePath = "./feature_flags.yaml"
                };
                var factory = new SplitFactory("localhost", splitConfig);
                var client = factory.Client();
                client.BlockUntilReady(30000);

                serviceCollection.AddSingleton(client);
            }
            else
            {
                var consumer = new RetsuConsumer(
                    new ConsumerConfiguration
                    {
                        ConnectionString = new Uri(config.RabbitUrl),
                        QueueName = "gateway",
                        ExchangeName = "consumer",
                        ConsumerAutoAck = false,
                        PrefetchCount = 25,
                    });

                await consumer.SubscribeAsync("MESSAGE_CREATE");
                await consumer.SubscribeAsync("MESSAGE_UPDATE");
                await consumer.SubscribeAsync("MESSAGE_DELETE");
                await consumer.SubscribeAsync("MESSAGE_DELETE_BULK");
                await consumer.SubscribeAsync("MESSAGE_REACTION_ADD");
                await consumer.SubscribeAsync("MESSAGE_REACTION_REMOVE");
                await consumer.SubscribeAsync("MESSAGE_REACTION_REMOVE_ALL");
                await consumer.SubscribeAsync("MESSAGE_REACTION_REMOVE_EMOJI");
                await consumer.SubscribeAsync("CHANNEL_CREATE");
                await consumer.SubscribeAsync("CHANNEL_DELETE");
                await consumer.SubscribeAsync("CHANNEL_PINS_UPDATE");
                await consumer.SubscribeAsync("CHANNEL_UPDATE");
                await consumer.SubscribeAsync("GUILD_CREATE");
                await consumer.SubscribeAsync("GUILD_DELETE");
                await consumer.SubscribeAsync("GUILD_UPDATE");
                await consumer.SubscribeAsync("GUILD_BAN_ADD");
                await consumer.SubscribeAsync("GUILD_BAN_REMOVE");
                await consumer.SubscribeAsync("GUILD_EMOJIS_UPDATE");
                await consumer.SubscribeAsync("GUILD_MEMBER_ADD");
                await consumer.SubscribeAsync("GUILD_MEMBER_REMOVE");
                await consumer.SubscribeAsync("GUILD_MEMBER_UPDATE");
                await consumer.SubscribeAsync("GUILD_ROLE_CREATE");
                await consumer.SubscribeAsync("GUILD_ROLE_DELETE");
                await consumer.SubscribeAsync("GUILD_ROLE_UPDATE");
                await consumer.SubscribeAsync("READY");
                await consumer.SubscribeAsync("RESUMED");

                serviceCollection.AddSingleton<IGateway>(consumer);
                serviceCollection.AddSingleton<IConnectionMultiplexer>(
                    await ConnectionMultiplexer.ConnectAsync(config.RedisConnectionString));
                serviceCollection.AddSingleton<ICacheClient, StackExchangeCacheClient>();
                serviceCollection.AddSingleton<IExtendedCacheClient, StackExchangeCacheClient>();

                var splitConfig = new Splitio.Services.Client.Classes.ConfigurationOptions();
                var factory = new SplitFactory(config.OptionalValues.SplitioSdkKey, splitConfig);
                var client = factory.Client();
                try
                {
                    client.BlockUntilReady(30000);
                }
                catch(TimeoutException)
                {
                    Log.Error("Couldn't initialize splitIO in time.");
                }
                serviceCollection.AddSingleton(client);
            }

            serviceCollection.AddSingleton<IDiscordClient, DiscordClient>();

            // Setup web services
            serviceCollection.AddSingleton<UrbanDictionaryApi>();
            serviceCollection.AddSingleton(x => new BunnyCDNClient(x.GetService<Config>().BunnyCdnKey));

            // Setup miscellanious services
            serviceCollection.AddSingleton<ConfigurationManager>();
            serviceCollection.AddSingleton(
                await BackgroundStore.LoadFromFileAsync("./resources/backgrounds.json"));

            ISentryClient sentryClient = null;
            if(!string.IsNullOrWhiteSpace(config.SharpRavenKey))
            {
                sentryClient = new SentryClient(
                    new SentryOptions
                    {
                        Dsn = new Dsn(config.SharpRavenKey)
                    });
            }
            serviceCollection.AddSingleton(s => sentryClient);
            
            serviceCollection.AddSingleton<IMessageWorker<IDiscordMessage>, MessageWorker>();
            serviceCollection.AddSingleton<TransactionEvents>();
            serviceCollection.AddSingleton(await BuildLocalesAsync());

            serviceCollection.AddScoped<ISettingsService, SettingsService>();
            serviceCollection.AddScoped<IUserService, UserService>();
            serviceCollection.AddScoped<IDailyService, DailyService>();
            serviceCollection.AddSingleton<AccountService>();
            serviceCollection.AddScoped<PastaService>();

            serviceCollection.AddSingleton<AchievementCollection>();
            serviceCollection.AddScoped<AchievementService>();

            serviceCollection.AddSingleton<ISchedulerService, SchedulerService>();
            serviceCollection.AddScoped<GuildService>();
            serviceCollection.AddScoped<MarriageService>();
            serviceCollection.AddScoped<IRpsService, RpsService>();
            serviceCollection.AddScoped<ILocalizationService, LocalizationService>();
            serviceCollection.AddScoped<PermissionService>();
            serviceCollection.AddScoped<ScopeService>();
            serviceCollection.AddScoped<ITransactionService, TransactionService>();
            serviceCollection.AddScoped<IBankAccountService, BankAccountService>();
            serviceCollection.AddSingleton<LotteryEventHandler>();
            serviceCollection.AddScoped<ILotteryService, LotteryService>();
            serviceCollection.AddSingleton<IOsuApiClient>(_ => config.OptionalValues?.OsuApiKey == null
                ? null
                : new OsuApiClientV1(config.OptionalValues.OsuApiKey));
                serviceCollection.AddScoped<BlackjackService>();
            serviceCollection.AddScoped<LeaderboardsService>();

            serviceCollection.AddSingleton(new PrefixCollectionBuilder()
                .AddAsDefault(new DynamicPrefixTrigger(">"))
                .Add(new PrefixTrigger("miki."))
                .Add(new MentionTrigger())
                .Build());

            serviceCollection.AddScoped<IPrefixService, PrefixService>();

            serviceCollection.AddSingleton(x => new CommandTreeBuilder(x)
                .AddCommandBuildStep(new ConfigurationManagerAdapter())
                .Create(Assembly.GetExecutingAssembly()));
        }

        public Task<IContext> CreateFromUserChannelAsync(IDiscordUser user, IDiscordChannel channel)
        {
            // TODO : Resolve this in a better way.
            DiscordMessage message = new DiscordMessage(
                new DiscordMessagePacket
                {
                    Author = new DiscordUserPacket
                    {
                        Avatar = user.AvatarId,
                        Discriminator = user.Discriminator,
                        Id = user.Id,
                        Username = user.Username,
                        IsBot = user.IsBot
                    },
                    ChannelId = channel.Id,
                    GuildId = (channel as IDiscordGuildChannel)?.GuildId,
                    Content = "no content",
                    Member = user is IDiscordGuildUser a
                        ? new DiscordGuildMemberPacket
                        {
                            JoinedAt = a.JoinedAt.UtcDateTime,
                            GuildId = a.GuildId,
                            Nickname = a.Nickname,
                            Roles = a.RoleIds.ToList(),
                        } : null,
                },
                Services.GetService<IDiscordClient>());
            return CreateFromMessageAsync(message);
        }
        /// <summary>
        /// Hacky temp function
        /// </summary>
        public async Task<IContext> CreateFromMessageAsync(IDiscordMessage message)
        {
            // TODO (velddev): Resolve this in a better way.
            var context = new ContextObject(Services);
            await new CorePipelineStage().CheckAsync(message, context, () => default);
            await new FetchDataStage().CheckAsync(message, context, () => default);
            return context;
        }

        private async Task<LocaleCollection> BuildLocalesAsync()
        {
            var collection = new LocaleCollection();
            var resourceFolder = "resources/locales";

            var files = Directory.GetFiles(resourceFolder);

            foreach(var fileName in files)
            {
                try
                {
                    var languageName = Path.GetFileNameWithoutExtension(fileName);
                    await using var json = new MemoryStream(await File.ReadAllBytesAsync(fileName));
                    var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(json);
                    var resourceManager = new FallbackResourceManager(new ResourceManager(dict));

                    collection.Add(new Locale(languageName, resourceManager));
                }
                catch(Exception ex)
                {
                    Log.Error($"Language {fileName} did not load correctly");
                    Log.Debug(ex.ToString());
                }
            }

            FallbackResourceManager.FallbackManager = collection.Get("eng").ResourceManager;
            return collection;
        }


        private static void CreateLogger()
        {
            var theme = new LogTheme();
            theme.SetColor(
                LogLevel.Information,
                new LogColor
                {
                    Foreground = ConsoleColor.White,
                    Background = 0
                });
            theme.SetColor(
                LogLevel.Error,
                new LogColor
                {
                    Foreground = ConsoleColor.Red,
                    Background = 0
                });
            theme.SetColor(
                LogLevel.Warning,
                new LogColor
                {
                    Foreground = ConsoleColor.Yellow,
                    Background = 0
                });

            new LogBuilder()
                .AddLogEvent((msg, lvl) =>
                {
                    if(lvl >= (LogLevel)Enum.Parse(typeof(LogLevel),
                           Environment.GetEnvironmentVariable(Constants.EnvLogLevel)))
                    {
                        Console.WriteLine(msg);
                    }
                })
                .SetLogHeader(msg => $"[{msg}]: ")
                .SetTheme(theme)
                .Apply();
        }

        private async Task Client_UserUpdated(IDiscordUser oldUser, IDiscordUser newUser)
        {
            using var scope = Services.CreateScope();
            if(oldUser.AvatarId != newUser.AvatarId)
            {
                await Utils.SyncAvatarAsync(newUser,
                        scope.ServiceProvider.GetService<IExtendedCacheClient>(),
                        scope.ServiceProvider.GetService<IUserService>(),
                        scope.ServiceProvider.GetService<AmazonS3Client>(),
                        scope.ServiceProvider.GetService<BunnyCDNClient>())
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
                (defaultChannel as IDiscordTextChannel).QueueMessage(
                    scope.ServiceProvider.GetService<MessageWorker>(), 
                    message: i.GetString("miki_join_message"));
            }

            List<string> allArgs = new List<string>();
            List<object> allParams = new List<object>();
            List<object> allExpParams = new List<object>();

            try
            {
                var members = (await arg.GetMembersAsync()).ToList();
                for(int i = 0; i < members.Count; i++)
                {
                    allArgs.Add($"(@p{i * 2}, @p{i * 2 + 1})");

                    allParams.Add(members.ElementAt(i).Id.ToDbLong());
                    allParams.Add(members.ElementAt(i).Username);

                    allExpParams.Add(arg.Id.ToDbLong());
                    allExpParams.Add(members.ElementAt(i).Id.ToDbLong());
                }

                await context.Database.ExecuteSqlRawAsync(
                    $"INSERT INTO dbo.\"Users\" (\"Id\", \"Name\") VALUES {string.Join(",", allArgs)} "
                    + "ON CONFLICT DO NOTHING",
                    allParams);

                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO dbo.\"LocalExperience\" (\"ServerId\", \"UserId\") "
                    + $"VALUES {string.Join(",", allArgs)} ON CONFLICT DO NOTHING",
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
