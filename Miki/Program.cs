using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Miki.API;
using Miki.Bot.Models;
using Miki.BunnyCDN;
using Miki.Cache;
using Miki.Cache.StackExchange;
using Miki.Configuration;
using Miki.Discord;
using Miki.Discord.Caching.Stages;
using Miki.Discord.Common;
using Miki.Discord.Gateway.Centralized;
using Miki.Discord.Gateway.Distributed;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Filters;
using Miki.Framework.Languages;
using Miki.Localization.Exceptions;
using Miki.Logging;
using Miki.Models;
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
		private static async Task Main()
		{
			Program p = new Program();

            var appBuilder = new MikiAppBuilder();
     
            await p.LoadServicesAsync(appBuilder);

            MikiApp app = appBuilder.Build();

			await p.LoadDiscord(app);

			p.LoadLocales();

			for (int i = 0; i < Global.Config.MessageWorkerCount; i++)
			{
				MessageBucket.AddWorker();
			}

			using (var c = new MikiContext())
			{
				List<User> bannedUsers = await c.Users.Where(x => x.Banned).ToListAsync();
				foreach (var u in bannedUsers)
				{
					app.GetService<EventSystem>().MessageFilter
						.Get<UserFilter>().Users.Add(u.Id.FromDbLong());
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

			typeList.ToList()
				.ForEach(t =>
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
			});

			Locale.SetDefaultLanguage("eng");
		}

        public async Task LoadServicesAsync(MikiAppBuilder app)
        {
            new LogBuilder()
                .AddLogEvent((msg, lvl) => {
                    if (lvl >= Global.Config.LogLevel) Console.WriteLine(msg);
                })
                .SetLogHeader((msg) => $"[{msg}]: ")
                .SetTheme(new LogTheme())
                .Apply();

            var cache = new StackExchangeCacheClient(
                new ProtobufSerializer(),
                await ConnectionMultiplexer.ConnectAsync(Global.Config.RedisConnectionString)
            );
            
            app.AddSingletonService<ICacheClient>(cache);
            app.AddSingletonService<IExtendedCacheClient>(cache);
            app.Services.AddDbContext<DbContext, MikiContext>(x => x.UseNpgsql(Global.Config.ConnString));

            if (!string.IsNullOrWhiteSpace(Global.Config.MikiApiBaseUrl) && !string.IsNullOrWhiteSpace(Global.Config.MikiApiKey))
            {
                app.AddSingletonService(new MikiApiClient(Global.Config.MikiApiKey));
            }
            else
            {
                Log.Warning("No Miki API parameters were supplied, ignoring Miki API.");
            }

            app.AddSingletonService<IApiClient>(new DiscordApiClient(Global.Config.Token, cache));

            if (Global.Config.SelfHosted)
            {
                var gatewayConfig = GatewayConfiguration.Default();
                gatewayConfig.ShardCount = 1;
                gatewayConfig.ShardId = 0;
                gatewayConfig.Token = Global.Config.Token;
                gatewayConfig.WebSocketClient = new BasicWebSocketClient();
                app.AddSingletonService<IGateway>(new CentralizedGatewayShard(gatewayConfig));
            }
            else
            {
                app.AddSingletonService<IGateway>(new DistributedGateway(new MessageClientConfiguration
                {
                    ConnectionString = new Uri(Global.Config.RabbitUrl.ToString()),
                    QueueName = "gateway",
                    ExchangeName = "consumer",
                    ConsumerAutoAck = false,
                    PrefetchCount = 25
                }));
            }

            app.AddSingletonService(new UrbanDictionaryAPI());
            app.AddSingletonService(new BunnyCDNClient(Global.Config.BunnyCdnKey));
            app.AddSingletonService(new ConfigurationManager());
            app.AddSingletonService(new EventSystem(new EventSystemConfig()
            {
                Developers = Global.Config.DeveloperIds,
            }));

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

        public async Task LoadDiscord(MikiApp app)
		{
            var cache = app.GetService<IExtendedCacheClient>();
            var gateway = app.GetService<IGateway>();

			new BasicCacheStage().Initialize(gateway, cache);

            var config = app.GetService<ConfigurationManager>();
            EventSystem eventSystem = app.GetService<EventSystem>();
            {
                //app.Discord.MessageCreate += eventSystem.OnMessageReceivedAsync;

                eventSystem.OnError += async (ex, context) =>
                {
                    if (ex is LocalizedException botEx)
                    {
                        await Utils.ErrorEmbedResource(context, botEx.LocaleResource)
                            .ToEmbed().QueueToChannelAsync(context.Channel);
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

                var handler = new SimpleCommandHandler(cache, commandMap);

                handler.AddPrefix(">", true, true);
                handler.AddPrefix("miki.");

                handler.OnMessageProcessed += async (cmd, msg, time) =>
                {
                    await Task.Yield();
                    Log.Message($"{cmd.Name} processed in {time}ms");
                };

                //eventSystem.AddCommandHandler(handler);

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
			if (oldUser.AvatarId != newUser.AvatarId)
			{
				await Utils.SyncAvatarAsync(newUser);
			}
		}

		private async Task Bot_MessageReceived(IDiscordMessage arg)
		{
            var user = await MikiApp.Instance.Discord.GetCurrentUserAsync();

			DogStatsd.Increment("messages.received");

			if (arg.Content.StartsWith($"<@!{user.Id}>") || arg.Content.StartsWith($"<@{user.Id}>"))
			{
                using (var context = new MikiContext())
                {
                    string msg = (await Locale.GetLanguageInstanceAsync(context, arg.ChannelId)).GetString("miki_join_message");
                    (await arg.GetChannelAsync()).QueueMessage(msg);
                }
			}

            if(Global.Config.LogLevel <= LogLevel.Debug)
            {
                Log.Debug($"Memory value: {GC.GetTotalMemory(true)}GB");
            }
		}

		private Task Client_LeftGuild(ulong guildId)
		{
			DogStatsd.Increment("guilds.left");
			return Task.CompletedTask;
		}

		private async Task Client_JoinedGuild(IDiscordGuild arg)
		{
			IDiscordChannel defaultChannel = await arg.GetDefaultChannelAsync();

			if (defaultChannel != null)
			{
                using (var context = new MikiContext())
                {
                    LocaleInstance i = await Locale.GetLanguageInstanceAsync(context, defaultChannel.Id);
                    (defaultChannel as IDiscordTextChannel).QueueMessage(i.GetString("miki_join_message"));
                }
			}

			//List<string> allArgs = new List<string>();
			//List<object> allParams = new List<object>();
			//List<object> allExpParams = new List<object>();

			//try
			//{
			//	for (int i = 0; i < arg.Members.Count; i++)
			//	{
			//		allArgs.Add($"(@p{i * 2}, @p{i * 2 + 1})");

			//		allParams.Add(arg.Members.ElementAt(i).Id.ToDbLong());
			//		allParams.Add(arg.Members.ElementAt(i).Username);

			//		allExpParams.Add(arg.Id.ToDbLong());
			//		allExpParams.Add(arg.Members.ElementAt(i).Id.ToDbLong());
			//	}

			//	using (var context = new MikiContext())
			//	{
			//		await context.Database.ExecuteSqlCommandAsync(
			//			$"INSERT INTO dbo.\"Users\" (\"Id\", \"Name\") VALUES {string.Join(",", allArgs)} ON CONFLICT DO NOTHING", allParams);

			//		await context.Database.ExecuteSqlCommandAsync(
			//			$"INSERT INTO dbo.\"LocalExperience\" (\"ServerId\", \"UserId\") VALUES {string.Join(",", allArgs)} ON CONFLICT DO NOTHING", allExpParams);

			//		await context.SaveChangesAsync();
			//	}
			//}
			//catch (Exception e)
			//{
			//	Log.Error(e.ToString());
			//}

			DogStatsd.Increment("guilds.joined");
		}
	}
}