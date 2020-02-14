namespace Miki
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Discord.Internal;
    using Framework.Commands.Localization.Services;
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
    using Miki.Discord.Common.Packets;
    using Miki.Discord.Common.Packets.API;
    using Miki.Discord.Gateway;
    using Miki.Discord.Rest;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Filters;
    using Miki.Framework.Commands.Permissions;
    using Miki.Framework.Commands.Prefixes;
    using Miki.Framework.Commands.Prefixes.Triggers;
    using Miki.Framework.Commands.Scopes;
    using Miki.Framework.Commands.Stages;
    using Miki.Framework.Events.Triggers;
    using Miki.Localization;
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;
    using Miki.Logging;
    using Miki.Models.Objects.Backgrounds;
    using Miki.Modules.Accounts.Services;
    using Miki.Modules.Logging;
    using Miki.Serialization;
    using Miki.Serialization.Protobuf;
    using Miki.Services;
    using Miki.Services.Achievements;
    using Miki.Services.Blackjack;
    using Miki.Services.Rps;
    using Miki.Services.Transactions;
    using Miki.UrbanDictionary;
    using Miki.Utility;
    using Retsu.Consumer;
    using Sentry;
    using StackExchange.Redis;
    using Veld.Osu;
    using Veld.Osu.V1;

    public class MikiBotApp : MikiApp
    {
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
                if(arg.Error.GetRootException() is LocalizedException botEx)
                {
                    await arg.Context.ErrorEmbedResource(botEx.LocaleResource)
                        .ToEmbed()
                        .QueueAsync(arg.Context, arg.Context.GetChannel());
                }
                else
                {
                    Log.Error(arg.Error);
                    var sentry = arg.Context.GetService<ISentryClient>();
                    sentry?.CaptureEvent(new SentryEvent(arg.Error));
                }
            }
        }

        public override IAsyncEventingExecutor<IDiscordMessage> ConfigurePipeline(IServiceProvider services)
        {
            LoadLocales(services);
            // TODO(velddev): Find a better place to load this.

            return new CommandPipelineBuilder(services)
                .UseStage(new CorePipelineStage())
                .UseFilters(
                    new BotFilter(),
                    new UserFilter())
                .UsePrefixes()
                .UseStage(new FetchDataStage())
                .UseLocalization()
                .UseArgumentPack()
                .UseCommandHandler()
                .UsePermissions()
                .UseScopes()
                .Build();
        }

        public override void Configure(ServiceCollection serviceCollection)
        {
            CreateLogger();

            // Setup Entity Framework
            {
                // TODO(velddev): Remove constant environment fetch.
                var connString = Environment.GetEnvironmentVariable(Constants.EnvConStr);
                if(connString == null)
                {
                    throw new InvalidOperationException("Connection string cannot be null");
                }

                serviceCollection.AddDbContext<MikiDbContext>(
                    x => x.UseNpgsql(connString, b => b.MigrationsAssembly("Miki.Bot.Models")));
                serviceCollection.AddDbContext<DbContext, MikiDbContext>(
                    x => x.UseNpgsql(connString, b => b.MigrationsAssembly("Miki.Bot.Models")));
            }

            
            serviceCollection.AddTransient<IUnitOfWork, UnitOfWork>();

            serviceCollection.AddSingleton<ConfigService>();
            serviceCollection.AddSingleton(
                x => x.GetService<ConfigService>()
                    .GetOrCreateAnyAsync(null)
                    .GetAwaiter().GetResult());

            serviceCollection.AddSingleton<ISerializer, ProtobufSerializer>();
            serviceCollection.AddSingleton<IConnectionMultiplexer>(x =>
            {
                var config = x.GetService<ConfigService>();
                // (velddev) blocks call, but resolves previous hack.
                var configValue = config.GetOrCreateAnyAsync(null)
                    .GetAwaiter().GetResult();
                return ConnectionMultiplexer.Connect(configValue.RedisConnectionString);
            });

            // Setup Redis
            serviceCollection.AddSingleton<ICacheClient, StackExchangeCacheClient>();
            serviceCollection.AddSingleton<IExtendedCacheClient, StackExchangeCacheClient>();

            serviceCollection.AddScoped<
                IRepositoryFactory<Achievement>, AchievementRepository.Factory>();

            serviceCollection.AddScoped(
                x => new MikiApiClient(x.GetService<Config>().MikiApiKey));

            // Setup Amazon CDN Client
            serviceCollection.AddSingleton(
                x => new AmazonS3Client(
                    x.GetService<Config>().CdnAccessKey,
                    x.GetService<Config>().CdnSecretKey,
                    new AmazonS3Config()
                    {
                        ServiceURL = x.GetService<Config>().CdnRegionEndpoint
                    }));

                    // Setup Discord
            {
                serviceCollection.AddSingleton<IApiClient>(
                    s => new DiscordApiClient(s.GetService<Config>().Token, s.GetService<ICacheClient>()));

                bool.TryParse(Environment.GetEnvironmentVariable(Constants.EnvSelfHost), out var selfHost);

                if(selfHost)
                {
                    serviceCollection.AddSingleton<IGateway>(
                        x => new GatewayCluster(
                            new GatewayProperties
                            {
                                ShardCount = 1,
                                ShardId = 0,
                                Token = x.GetService<Config>().Token,
                                Compressed = true,
                                AllowNonDispatchEvents = true
                            }));
                }
                else
                {
                    serviceCollection.AddSingleton<IGateway>(x => new RetsuConsumer(
                        new ConsumerConfiguration
                        {
                            ConnectionString = new Uri(x.GetService<Config>().RabbitUrl),
                            QueueName = "gateway",
                            ExchangeName = "consumer",
                            ConsumerAutoAck = false,
                            PrefetchCount = 25,
                        }));
                }

                serviceCollection.AddSingleton<IDiscordClient, DiscordClient>();
            }

            // Setup web services
            serviceCollection.AddSingleton(new UrbanDictionaryApi());
            serviceCollection.AddSingleton(x => new BunnyCDNClient(x.GetService<Config>().BunnyCdnKey));

            // Setup miscellanious services
            {
                serviceCollection.AddSingleton<ConfigurationManager>();
                serviceCollection.AddSingleton<BackgroundStore>();

                serviceCollection.AddSingleton<ISentryClient>(
                    x => new SentryClient(
                        new SentryOptions
                        {
                            Dsn = new Dsn(x.GetService<Config>().SharpRavenKey)
                        }));

                serviceCollection.AddSingleton<IMessageWorker<IDiscordMessage>, MessageWorker>();
                serviceCollection.AddSingleton<TransactionEvents>();

                serviceCollection.AddScoped<IUserService, UserService>();
                serviceCollection.AddSingleton<AccountService>();
                serviceCollection.AddScoped<PastaService>();

                serviceCollection.AddSingleton<AchievementCollection>();
                serviceCollection.AddScoped<AchievementService>();

                serviceCollection.AddScoped<MarriageService>();
                serviceCollection.AddScoped<RpsService>();
                serviceCollection.AddScoped<ILocalizationService, LocalizationService>();
                serviceCollection.AddScoped<PermissionService>();
                serviceCollection.AddScoped<ScopeService>();
                serviceCollection.AddScoped<ITransactionService, TransactionService>();
                serviceCollection.AddSingleton<IOsuApiClient>(
                    x =>
                    {
                        var config = x.GetService<Config>();
                        if(config.OptionalValues?.OsuApiKey == null)
                        {
                            return null;
                        }
                        return new OsuApiClientV1(config.OptionalValues.OsuApiKey);
                    });
                serviceCollection.AddScoped<BlackjackService>();
                serviceCollection.AddScoped<LeaderboardsService>();

                serviceCollection.AddSingleton(
                    new PrefixCollection<IDiscordMessage>
                    {
                        new PrefixTrigger(">", true, true),
                        new PrefixTrigger("miki.", false),
                        new MentionTrigger()
                    });
                serviceCollection.AddSingleton<PrefixService<IDiscordMessage>>();
            }

            serviceCollection.AddSingleton(x =>
                new CommandTreeBuilder(x)
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
                            JoinedAt = a.JoinedAt.ToUnixTimeSeconds(),
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
            await new CorePipelineStage()
                .CheckAsync(message, context, () => default);
            await new FetchDataStage()
                .CheckAsync(message, context, () => default);
            return context;
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
                        scope.ServiceProvider.GetService<AmazonS3Client>())
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
