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
using Miki.Framework.Events;
using Miki.Framework.Events.Filters;
using Miki.Framework.Events.Triggers;
using Miki.Framework.Languages;
using Miki.Localization.Exceptions;
using Miki.Logging;
using Miki.Models.Objects.Backgrounds;
using Miki.Net.WebSockets;
using Miki.Serialization.Protobuf;
using Miki.UrbanDictionary;
using SharpRaven;
using SharpRaven.Data;
using StackExchange.Redis;
using StatsdClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;

namespace Miki
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            Program p = new Program();

            if (args.Length > 0)
            {
                if (args.Any(x => x.ToLowerInvariant() == "--migrate" && x.ToLowerInvariant() == "-m"))
                {
                    await new MikiDbContextFactory().CreateDbContext(new string[] { }).Database.MigrateAsync();
                    return;
                }
            }

            var appBuilder = new MikiAppBuilder();

            await p.LoadServicesAsync(appBuilder);

            MikiApp app = appBuilder.Build();

            await p.LoadDiscord(app);

            p.LoadLocales();

            for (int i = 0; i < Global.Config.MessageWorkerCount; i++)
            {
                MessageBucket.AddWorker();
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
                    app.GetService<EventSystem>().MessageFilter
                        .Get<UserFilter>().Users.Add(u.UserId.FromDbLong());
                }
            }

            await Task.Delay(-1);
        }

        private void LoadLocales()
        {
            string nameSpace = "Miki.Languages";

            var typeList = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && t.Namespace == nameSpace);

            foreach (var t in typeList)
            {
                try
                {
                    string l = t.Name.ToLowerInvariant();
                    ResourceManager resources = new ResourceManager($"Miki.Languages.{l}", t.Assembly);
                    Locale.LoadLanguage(l, resources, resources.GetString("current_language_name"));
                }
                catch (Exception ex)
                {
                    Log.Error($"Language {t.Name} did not load correctly");
                    Log.Debug(ex.ToString());
                }
            }

            Locale.SetDefaultLanguage("eng");
        }

        public async Task LoadServicesAsync(MikiAppBuilder app)
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
                app.AddSingletonService(new EventSystem());
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

        public async Task LoadDiscord(MikiApp app)
        {
            var cache = app.GetService<IExtendedCacheClient>();
            var gateway = app.GetService<IGateway>();

            new BasicCacheStage().Initialize(gateway, cache);

            var config = app.GetService<ConfigurationManager>();
            EventSystem eventSystem = app.GetService<EventSystem>();
            {
                app.Discord.MessageCreate += eventSystem.OnMessageReceivedAsync;

                eventSystem.OnError += async (ex, context) =>
                {
                    if (ex is LocalizedException botEx)
                    {
                        if (context is ICommandContext m)
                        {
                            await Utils.ErrorEmbedResource(m, botEx.LocaleResource)
                                .ToEmbed().QueueToChannelAsync(m.Channel);
                        }
                    }
                    else
                    {
                        Log.Error(ex);
                        await app.GetService<RavenClient>().CaptureAsync(new SentryEvent(ex));
                    }
                };

                eventSystem.MessageFilter.AddFilter(new BotFilter());
                eventSystem.MessageFilter.AddFilter(new UserFilter());

                var commandMap = new Framework.Events.CommandMap();
                commandMap.OnModuleLoaded += (module) =>
                {
                    config.RegisterType(module.GetReflectedInstance().GetType(), module.GetReflectedInstance());
                };

                eventSystem.AddMessageTrigger(new PrefixTrigger(">", true, true));
                eventSystem.AddMessageTrigger(new PrefixTrigger("miki.", false));
                eventSystem.AddMessageTrigger(new MentionTrigger());

                var handler = new SimpleCommandHandler(commandMap);
                handler.OnMessageProcessed += async (cmd, msg, time) =>
                {
                    await Task.Yield();
                    Log.Message($"{cmd.Name} processed in {time}ms");
                };

                eventSystem.AddCommandHandler(handler);

                commandMap.RegisterAttributeCommands();
                commandMap.Install(eventSystem);
            }

            string configFile = Environment.CurrentDirectory + Config.MikiConfigurationFile;

            if (File.Exists(configFile))
            {
                await config.ImportAsync(
                    new JsonSerializationProvider(),
                    configFile
                );
            }

            await config.ExportAsync(
                new JsonSerializationProvider(),
                configFile
            );

            app.Discord.MessageCreate += Bot_MessageReceived;

            app.Discord.GuildJoin += Client_JoinedGuild;
            app.Discord.GuildLeave += Client_LeftGuild;
            app.Discord.UserUpdate += Client_UserUpdated;

            await gateway.StartAsync();
        }

        private async Task Client_UserUpdated(IDiscordUser oldUser, IDiscordUser newUser)
        {
            using (var scope = MikiApp.Instance.Services.CreateScope())
            {
                if (oldUser.AvatarId != newUser.AvatarId)
                {
                    await Utils.SyncAvatarAsync(newUser, scope.ServiceProvider.GetService<IExtendedCacheClient>(), scope.ServiceProvider.GetService<MikiDbContext>());
                }
            }
        }


        private Task Bot_MessageReceived(IDiscordMessage arg)
        {
            DogStatsd.Increment("messages.received");
            return Task.CompletedTask;
        }

        private Task Client_LeftGuild(ulong guildId)
        {
            DogStatsd.Increment("guilds.left");
            return Task.CompletedTask;
        }

        private async Task Client_JoinedGuild(IDiscordGuild arg)
        {
            using (var scope = MikiApp.Instance.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<DbContext>();

                IDiscordChannel defaultChannel = await arg.GetDefaultChannelAsync();
                if (defaultChannel != null)
                {
                    LocaleInstance i = await Locale.GetLanguageInstanceAsync(context, defaultChannel.Id);
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

                DogStatsd.Increment("guilds.joined");
            }
        }
    }
}