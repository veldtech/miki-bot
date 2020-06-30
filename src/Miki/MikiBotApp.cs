using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Miki.Discord.Internal;
using Miki.Framework.Commands.Localization.Services;
using Miki.Functional;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Miki.Accounts;
using Miki.Adapters;
using Miki.API;
using Miki.API.Backgrounds;
using Miki.Bot.Models;
using Miki.Bot.Models.Repositories;
using Miki.Cache;
using Miki.Cache.InMemory;
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
using Miki.Modules.CustomCommands.Services;
using Miki.Modules.Internal.Routines;
using Miki.Serialization;
using Miki.Serialization.Protobuf;
using Miki.Services;
using Miki.Services.Achievements;
using Miki.Services.Dailies;
using Miki.Services.Lottery;
using Miki.Services.Marriages;
using Miki.Services.Pasta;
using Miki.Services.Reddit;
using Miki.Services.Rps;
using Miki.Services.Scheduling;
using Miki.Services.Settings;
using Miki.Services.Transactions;
using Miki.UrbanDictionary;
using Miki.Utility;
using MiScript;
using Retsu.Consumer;
using Sentry;
using Splitio.Services.Client.Classes;
using Splitio.Services.Client.Interfaces;
using Veld.Osu;
using Veld.Osu.V1;

namespace Miki
{
    public class MikiBotApp : MikiApp
    {
        private readonly IStartupConfiguration configuration;

        public MikiBotApp(IStartupConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public override ProviderCollection ConfigureProviders(
            IServiceProvider services, IAsyncEventingExecutor<IDiscordMessage> pipeline)
        {
            var _ = services.GetService<DatadogRoutine>(); // Eager loading
            var discordClient = services.GetService<IDiscordClient>();
            discordClient.GuildJoin += ClientJoinedGuildAsync;  

            discordClient.MessageCreate += async e => await pipeline.ExecuteAsync(e);
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

        public override async Task ConfigureAsync(ServiceCollection serviceCollection)
        {
            CreateLogger(configuration.LogLevel);

            if(string.IsNullOrWhiteSpace(configuration.ConnectionString))
            {
                throw new InvalidOperationException("Connection string cannot be null");
            }

            serviceCollection.AddDbContext<MikiDbContext>(
                x => x.UseNpgsql(
                        configuration.ConnectionString, 
                        b => b.MigrationsAssembly("Miki.Bot.Models"))
                    .EnableDetailedErrors());
            serviceCollection.AddDbContext<DbContext, MikiDbContext>(
                x => x.UseNpgsql(
                        configuration.ConnectionString, 
                        b => b.MigrationsAssembly("Miki.Bot.Models"))
                    .EnableDetailedErrors());

            serviceCollection.AddMiScript(builder =>
            {
                builder.AddCompiler();
            });

            serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();
            serviceCollection.AddSingleton(configuration.Configuration);
            serviceCollection.AddSingleton<ISerializer, ProtobufSerializer>();
            
            serviceCollection.AddScoped<
                IRepositoryFactory<Achievement>, AchievementRepository.Factory>();

            serviceCollection.AddScoped(x => new MikiApiClient(x.GetService<Config>().MikiApiKey));

            // Setup Discord
            serviceCollection.AddSingleton<IApiClient>(
                s => new DiscordApiClient(s.GetService<Config>().Token, s.GetService<ICacheClient>()));

            if(configuration.IsSelfHosted)
            {
                serviceCollection.AddSingleton<IGateway>(
                    new GatewayShard(
                        new GatewayProperties
                        {
                            ShardCount = 1,
                            ShardId = 0,
                            Token = configuration.Configuration.Token,
                            AllowNonDispatchEvents = true,
                            Intents = GatewayIntents.AllDefault | GatewayIntents.GuildMembers
                        }));    

                serviceCollection.AddSingleton<ICacheClient, InMemoryCacheClient>();
                serviceCollection.AddSingleton<IExtendedCacheClient, InMemoryCacheClient>();

                var splitConfig = new ConfigurationOptions
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
                        ConnectionString = new Uri(configuration.Configuration.RabbitUrl),
                        QueueName = "gateway",
                        ExchangeName = "consumer",
                        ConsumerAutoAck = false,
                        PrefetchCount = 25,
                    },
                    new Retsu.Consumer.Models.QueueConfiguration
                    {
                        ConnectionString = new Uri(configuration.Configuration.RabbitUrl),
                        QueueName = "gateway-command",
                        ExchangeName = "consumer",
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

                serviceCollection.AddSingleton(
                    new RedisConnectionPool(configuration.Configuration.RedisConnectionString));

                serviceCollection.AddTransient(
                    x => x.GetRequiredService<RedisConnectionPool>().Get());

                serviceCollection.AddTransient<ICacheClient, StackExchangeCacheClient>();
                serviceCollection.AddTransient<IExtendedCacheClient, StackExchangeCacheClient>();

                ISplitClient client = null;
                if(!string.IsNullOrEmpty(configuration.Configuration.OptionalValues?.SplitioSdkKey))
                {
                    var splitConfig = new ConfigurationOptions();
                    var factory = new SplitFactory(
                        configuration.Configuration.OptionalValues?.SplitioSdkKey, splitConfig);
                    client = factory.Client();
                    try
                    {
                        client.BlockUntilReady(30000);
                    }
                    catch(TimeoutException)
                    {
                        Log.Error("Couldn't initialize splitIO in time.");
                    }
                }

                serviceCollection.AddSingleton(x => client);
            }

            serviceCollection.AddSingleton<IDiscordClient, DiscordClient>();

            // Setup web services
            serviceCollection.AddSingleton<UrbanDictionaryApi>();
            
            // Setup miscellanious services
            serviceCollection.AddSingleton<ConfigurationManager>();
            serviceCollection.AddSingleton<IBackgroundStore>(
                await BackgroundStore.LoadFromFileAsync("./resources/backgrounds.json"));

            ISentryClient sentryClient = null;
            if(!string.IsNullOrWhiteSpace(configuration.Configuration.SharpRavenKey))
            {
                sentryClient = new SentryClient(
                    new SentryOptions
                    {
                        Dsn = new Dsn(configuration.Configuration.SharpRavenKey)
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

            serviceCollection.AddSingleton<RedditService>();
            serviceCollection.AddSingleton<AchievementCollection>();
            serviceCollection.AddScoped<AchievementService>();
            serviceCollection.AddScoped<ICustomCommandsService, CustomCommandsService>();

            serviceCollection.AddSingleton<ISchedulerService, SchedulerService>();
            serviceCollection.AddScoped<IGuildService, GuildService>();
            serviceCollection.AddScoped<MarriageService>();
            serviceCollection.AddScoped<IRpsService, RpsService>();
            serviceCollection.AddScoped<ILocalizationService, LocalizationService>();
            serviceCollection.AddScoped<PermissionService>();
            serviceCollection.AddScoped<ScopeService>();
            serviceCollection.AddScoped<ITransactionService, TransactionService>();
            serviceCollection.AddScoped<IBankAccountService, BankAccountService>();
            serviceCollection.AddSingleton<LotteryEventHandler>();
            serviceCollection.AddScoped<ILotteryService, LotteryService>();
            serviceCollection.AddScoped<IBackgroundService, BackgroundService>();
            serviceCollection.AddSingleton<IOsuApiClient>(
                _ => configuration.Configuration.OptionalValues?.OsuApiKey == null
                    ? null
                    : new OsuApiClientV1(configuration.Configuration.OptionalValues.OsuApiKey));
                serviceCollection.AddScoped<BlackjackService>();
            serviceCollection.AddScoped<LeaderboardsService>();

            serviceCollection.AddSingleton(new PrefixCollectionBuilder()
                .AddAsDefault(new DynamicPrefixTrigger(">"))
                .Add(new PrefixTrigger("miki."))
                .Add(new MentionTrigger())
                .Build());

            serviceCollection.AddScoped<IPrefixService, PrefixService>();

            serviceCollection.AddSingleton(
                x => new CommandTreeBuilder(x).Create(Assembly.GetExecutingAssembly()));

            serviceCollection.AddSingleton<CommandTreeService>();

            serviceCollection.AddSingleton<IAsyncEventingExecutor<IDiscordMessage>>(
                services => new CommandPipelineBuilder(services)
                    .UseStage(new CorePipelineStage())
                    .UseFilters(new BotFilter(), new UserFilter())
                    .UsePrefixes()
                    .UseStage(new FetchDataStage())
                    .UseLocalization()
                    .UseArgumentPack()
                    .UseCommandHandler()
                    .UsePermissions()
                    .UseScopes()
                    .Build());

            serviceCollection.AddSingleton<DatadogRoutine>();
        }

        public async Task<ContextObject> CreateFromUserChannelAsync(IDiscordUser user, IDiscordChannel channel)
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
            var context = new ContextObject(Services);
            await new CorePipelineStage().CheckAsync(message, context, () => default);
            await new FetchDataStage().CheckAsync(message, context, () => default);
            return context;
        }
        
        /// <summary>
        /// Hacky temp function
        /// </summary>
        public async Task CreateFromMessageAsync(
            IDiscordMessage message, Func<IContext, Task> scope)
        {
            // TODO (velddev): Resolve this in a better way.
            using var context = new ContextObject(Services);
            await new CorePipelineStage().CheckAsync(message, context, () => default);
            await new FetchDataStage().CheckAsync(message, context, () => default);
            await new LocalizationPipelineStage(Services.GetService<ILocalizationService>())
                .CheckAsync(message, context, () => default);
            await scope(context);
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

        private static void CreateLogger(LogLevel loglevel)
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

            try
            {
                new LogBuilder()
                    .AddLogEvent((msg, lvl) =>
                    {
                        if(lvl >= loglevel)
                        {
                            Console.WriteLine(msg);
                        }
                    })
                    .SetLogHeader(msg => $"[{msg}]: ")
                    .SetTheme(theme)
                    .Apply();
            } catch(UnauthorizedAccessException) { } // Means log is set up already.
        }

        private async Task ClientJoinedGuildAsync(IDiscordGuild arg)
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
                    scope.ServiceProvider.GetService<IMessageWorker<IDiscordMessage>>(), 
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

                    allParams.Add((long)members.ElementAt(i).Id);
                    allParams.Add(members.ElementAt(i).Username);

                    allExpParams.Add((long)arg.Id);
                    allExpParams.Add((long)members.ElementAt(i).Id);
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
