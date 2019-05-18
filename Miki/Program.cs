using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Miki.API;
using Miki.Bot.Models;
using Miki.Bot.Models.Models.User;
using Miki.BunnyCDN;
using Miki.Cache;
using Miki.Cache.StackExchange;
using Miki.Configuration;
using Miki.Discord;
using Miki.Discord.Caching.Stages;
using Miki.Discord.Common;
using Miki.Discord.Gateway;
using Miki.Discord.Gateway.Distributed;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Arguments;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Filters;
using Miki.Framework.Commands.Filters.Filters;
using Miki.Framework.Commands.Localization;
using Miki.Framework.Commands.Pipelines;
using Miki.Framework.Events;
using Miki.Framework.Events.Triggers;
using Miki.Localization;
using Miki.Localization.Exceptions;
using Miki.Logging;
using Miki.Models.Objects.Backgrounds;
using Miki.Serialization.Protobuf;
using Miki.UrbanDictionary;
using SharpRaven;
using SharpRaven.Data;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;

namespace Miki
{
    public class Program
    {
        private static async Task Main()
        {
            var appBuilder = new MikiAppBuilder();
            await LoadServicesAsync(appBuilder);
            MikiApp app = appBuilder.Build();

            var cmdTree = await BuildCommandMapAsync(app);
            var commands = BuildPipeline(app, cmdTree);
            await LoadDiscordAsync(app, commands);
            await LoadFiltersAsync(app, commands);
            LoadLocales(commands);

            for (int i = 0; i < Global.Config.MessageWorkerCount; i++)
            {
                MessageBucket.AddWorker();
            }
            await Task.Delay(-1);
        }

        private static async Task<CommandTree> BuildCommandMapAsync(MikiApp app)
        {
            CommandTreeBuilder commandBuilder = new CommandTreeBuilder();
            commandBuilder.OnContainerLoaded += (c) =>
            {
                var config = app.GetService<ConfigurationManager>();
                config.RegisterType(c.GetType(), c.Instance);
                return Task.CompletedTask;
            };
            return await commandBuilder
                .CreateAsync(Assembly.GetEntryAssembly());
        }

        private static CommandPipeline BuildPipeline(MikiApp app, CommandTree map)
            => new CommandPipelineBuilder(app)
                .UseStage(new CorePipelineStage())
                .UseFilters(
                    new BotFilter(),
                    new UserFilter()
                )
                .UsePrefixes(
                    new PrefixTrigger(">", true, true),
                    new PrefixTrigger("miki.", false),
                    new MentionTrigger()
                )
                .UseLocalization()
                .UseArgumentPack()
                .UseCommandHandler(map)
                .UseStates()
                .UsePermissions()
                .Build();

        private static void LoadLocales(CommandPipeline app)
        {
            string nameSpace = "Miki.Languages";

            var typeList = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && t.Namespace == nameSpace);

            var locale = app.PipelineStages
                .Where(x => x is LocalizationPipelineStage)
                .Select(x => x as LocalizationPipelineStage)
                .FirstOrDefault();

            foreach (var t in typeList)
            {
                try
                {
                    string languageName = t.Name.ToLowerInvariant();

                    ResourceManager resources = new ResourceManager(
                        $"Miki.Languages.{languageName}",
                        t.Assembly);

                    IResourceManager resourceManager = new ResxResourceManager(
                        resources);

                    locale.LoadLanguage(
                        languageName,
                        resourceManager,
                        resourceManager.GetString("current_language_name"));
                }
                catch (Exception ex)
                {
                    Log.Error($"Language {t.Name} did not load correctly");
                    Log.Debug(ex.ToString());
                }
            }

            locale.SetDefaultLanguage("eng");
        }

        public static async Task LoadServicesAsync(MikiAppBuilder app)
        {
            new LogBuilder()
                .AddLogEvent((msg, lvl) =>
                {
                    if (lvl >= Global.Config.LogLevel)
                        Console.WriteLine(msg);
                })
                .SetLogHeader((msg) => $"[{msg}]: ")
                .SetTheme(new LogTheme())
                .Apply();

            var cache = new StackExchangeCacheClient(
                new ProtobufSerializer(),
                await ConnectionMultiplexer.ConnectAsync(Global.Config.RedisConnectionString)
            );

            // Setup Redis
            {
                app.AddSingletonService<ICacheClient>(cache);
                app.AddSingletonService<IExtendedCacheClient>(cache);
            }

            // Setup Entity Framework
            {
                app.Services.AddDbContext<MikiDbContext>(x
                    => x.UseNpgsql(Global.Config.ConnString, b => b.MigrationsAssembly("Miki.Bot.Models")));
                app.Services.AddDbContext<DbContext, MikiDbContext>(x
                    => x.UseNpgsql(Global.Config.ConnString, b => b.MigrationsAssembly("Miki.Bot.Models")));
            }

            // Setup Miki API
            {
                if (!string.IsNullOrWhiteSpace(Global.Config.MikiApiBaseUrl) && !string.IsNullOrWhiteSpace(Global.Config.MikiApiKey))
                {
                    app.AddSingletonService(new MikiApiClient(Global.Config.MikiApiKey));
                }
                else
                {
                    Log.Warning("No Miki API parameters were supplied, ignoring Miki API.");
                }
            }

            // Setup Discord
            {
                app.AddSingletonService<IApiClient>(new DiscordApiClient(Global.Config.Token, cache));
                if (Global.Config.SelfHosted)
                {
                    var gatewayConfig = new GatewayProperties();
                    gatewayConfig.ShardCount = 1;
                    gatewayConfig.ShardId = 0;
                    gatewayConfig.Token = Global.Config.Token;
                    gatewayConfig.Compressed = true;
                    gatewayConfig.AllowNonDispatchEvents = true;
                    app.AddSingletonService<IGateway>(new GatewayCluster(gatewayConfig));
                }
                else
                {
                    app.AddSingletonService<IGateway>(new DistributedGateway(new MessageClientConfiguration
                    {
                        ConnectionString = new Uri(Global.Config.RabbitUrl.ToString()),
                        QueueName = "gateway",
                        ExchangeName = "consumer",
                        ConsumerAutoAck = false,
                        PrefetchCount = 25,
                    }));
                }
            }

            // Setup web services
            {
                app.AddSingletonService(new UrbanDictionaryAPI());
                app.AddSingletonService(new BunnyCDNClient(Global.Config.BunnyCdnKey));
            }

            // Setup miscellanious services
            {
                app.AddSingletonService(new ConfigurationManager());
                app.AddSingletonService(new BackgroundStore());

                if (!string.IsNullOrWhiteSpace(Global.Config.SharpRavenKey))
                {
                    app.AddSingletonService(new RavenClient(Global.Config.SharpRavenKey));
                }
                else
                {
                    Log.Warning("Sentry.io key not provided, ignoring distributed error logging...");
                }
            }
        }

        public static async Task LoadDiscordAsync(MikiApp app, CommandPipeline pipeline)
        {
            var cache = app.GetService<IExtendedCacheClient>();
            var gateway = app.GetService<IGateway>();

            new BasicCacheStage().Initialize(gateway, cache);

            var config = app.GetService<ConfigurationManager>();

            //string configFile = Environment.CurrentDirectory + Config.MikiConfigurationFile;

            //if (File.Exists(configFile))
            //{
            //    await config.ImportAsync(
            //        new JsonSerializationProvider(),
            //        configFile
            //    );
            //}

            //await config.ExportAsync(
            //    new JsonSerializationProvider(),
            //    configFile
            //);

            app.Discord.MessageCreate += pipeline.CheckAsync;
            pipeline.OnError += OnErrorAsync;
            app.Discord.GuildJoin += Client_JoinedGuild;
            app.Discord.UserUpdate += Client_UserUpdated;

            await gateway.StartAsync();
        }

        public static async Task LoadFiltersAsync(MikiApp app, CommandPipeline pipeline)
        {
            var filters = pipeline
                .GetPipelineStagesOfType<FilterPipelineStage>()
                .FirstOrDefault();
            if (filters == null)
            {
                Log.Warning("Filters not set up in command pipeline.");
                return;
            }

            var userFilter = filters
                .GetFilterOfType<UserFilter>();
            if (userFilter == null)
            {
                Log.Warning("User filter not set up in command pipeline.");
                return;
            }

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetService<MikiDbContext>();

                List<IsBanned> bannedUsers = await context.IsBanned
                    .Where(x => x.ExpirationDate > DateTime.UtcNow)
                    .ToListAsync();

                foreach (var u in bannedUsers)
                {
                    userFilter.Users.Add(u.UserId);
                }
            }
        }

        private static async Task Client_UserUpdated(IDiscordUser oldUser, IDiscordUser newUser)
        {
            using (var scope = MikiApp.Instance.Services.CreateScope())
            {
                if (oldUser.AvatarId != newUser.AvatarId)
                {
                    await Utils.SyncAvatarAsync(newUser, scope.ServiceProvider.GetService<IExtendedCacheClient>(), scope.ServiceProvider.GetService<MikiDbContext>());
                }
            }
        }

        private static async Task Client_JoinedGuild(IDiscordGuild arg)
        {
            using (var scope = MikiApp.Instance.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<DbContext>();

                IDiscordChannel defaultChannel = await arg.GetDefaultChannelAsync();
                if (defaultChannel != null)
                {
                    var locale = scope.ServiceProvider.GetService<LocalizationPipelineStage>();
                    IResourceManager i = await locale.GetLocaleAsync(
                        scope.ServiceProvider,
                        (long)defaultChannel.Id);
                    (defaultChannel as IDiscordTextChannel).QueueMessage(i.GetString("miki_join_message"));
                }

                List<string> allArgs = new List<string>();
                List<object> allParams = new List<object>();
                List<object> allExpParams = new List<object>();

                try
                {
                    var members = await arg.GetMembersAsync();
                    for (int i = 0; i < members.Length; i++)
                    {
                        allArgs.Add($"(@p{i * 2}, @p{i * 2 + 1})");

                        allParams.Add(members.ElementAt(i).Id.ToDbLong());
                        allParams.Add(members.ElementAt(i).Username);

                        allExpParams.Add(arg.Id.ToDbLong());
                        allExpParams.Add(members.ElementAt(i).Id.ToDbLong());
                    }

                    await context.Database.ExecuteSqlCommandAsync(
                        $"INSERT INTO dbo.\"Users\" (\"Id\", \"Name\") VALUES {string.Join(",", allArgs)} ON CONFLICT DO NOTHING", allParams);

                    await context.Database.ExecuteSqlCommandAsync(
                        $"INSERT INTO dbo.\"LocalExperience\" (\"ServerId\", \"UserId\") VALUES {string.Join(",", allArgs)} ON CONFLICT DO NOTHING", allExpParams);

                    await context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }
        }

        private static async Task OnErrorAsync(Exception exception, IContext context)
        {
            if (exception is LocalizedException botEx)
            {
                await Utils.ErrorEmbedResource(context, botEx.LocaleResource)
                    .ToEmbed().QueueAsync(context.GetChannel());
            }
            else
            {
                Log.Error(exception);
                await context.GetService<RavenClient>()
                    .CaptureAsync(new SentryEvent(exception));
            }
        }
    }
}