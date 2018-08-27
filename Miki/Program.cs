using Microsoft.EntityFrameworkCore;
using Miki.API;
using Miki.Cache;
using Miki.Cache.StackExchange;
using Miki.Common;
using Miki.Configuration;
using Miki.Discord;
using Miki.Discord.Caching.Stages;
using Miki.Discord.Common;
using Miki.Discord.Common.Packets;
using Miki.Discord.Gateway.Distributed;
using Miki.Discord.Internal;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Commands;
using Miki.Framework.Events.Filters;
using Miki.Framework.Exceptions;
using Miki.Framework.Language;
using Miki.Framework.Languages;
using Miki.Logging;
using Miki.Models;
using Miki.Serialization.Protobuf;
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
		public static DateTime timeSinceStartup;

		static async Task Main()
		{
			

			Program p = new Program();

			timeSinceStartup = DateTime.Now;

			p.InitLogging();

			p.LoadLocales();

			await p.LoadDiscord();

			if (!string.IsNullOrWhiteSpace(Global.Config.MikiApiKey))
			{
				Global.MikiApi = new MikiApi(Global.Config.MikiApiBaseUrl, Global.Config.MikiApiKey);
			}

			for (int i = 0; i < Global.Config.MessageWorkerCount; i++)
			{
				MessageBucket.AddWorker();
			}

			using (var c = new MikiContext())
			{
				List<User> bannedUsers = await c.Users.Where(x => x.Banned).ToListAsync();
				foreach (var u in bannedUsers)
				{
					Global.Client.GetAttachedObject<EventSystem>().MessageFilter
						.Get<UserFilter>().Users.Add(u.Id.FromDbLong());
				}
			}
			await Task.Delay(-1);
		}

		private void LoadLocales()
		{
			string nspace = "Miki.Languages";

			var q = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && t.Namespace == nspace);

			// en_US -> en-us
			q.ToList().ForEach(t =>
			{
				try
				{
					string[] l = t.Name.Split('_');
					l[1] = l[1].ToUpper();

					ResourceManager resources = new ResourceManager($"Miki.Languages.{string.Join("-", l)}", t.Assembly);
					string languageName = resources.GetString("current_language_name");
					Locale.LoadLanguage(t.Name.ToLower().Replace("_", "-"), languageName, resources);
				}
				catch
				{
					Log.Error($"Language {t.Name} did not load correctly");
				}
			});
		}

		public async Task LoadDiscord()
		{
			StackExchangeCachePool pool = new StackExchangeCachePool(
				new ProtobufSerializer(),
				ConfigurationOptions.Parse(Global.Config.RedisConnectionString)
			);


			var client = new DistributedGateway(new MessageClientConfiguration
			{
				ConnectionString = new Uri(Global.Config.RabbitUrl.ToString()),
				QueueName = "gateway",
				ExchangeName = "consumer",
				ConsumerAutoAck = false,
				PrefetchCount = 25
			});

			Global.Client = new Bot(client, pool, new ClientInformation()
			{
				Name = "Miki",
				Version = "0.7",
				ShardCount = Global.Config.ShardCount,
				DatabaseConnectionString = Global.Config.ConnString,
				Token = Global.Config.Token
			});

			(Global.Client.Client.ApiClient as DiscordApiClient).HttpClient.OnRequestComplete += (method, uri) =>
			{
				DogStatsd.Histogram("discord.http.requests", uri, 1, new string[] { $"http_method:{method}" });
			};

			new BasicCacheStage().Initialize(Global.Client.CacheClient);
			
			EventSystem eventSystem = new EventSystem(new EventSystemConfig()
			{
				Developers = Global.Config.DeveloperIds,
			});

			eventSystem.OnError += async (ex, context) =>
			{
				if (ex is BotException botEx)
				{
					Utils.ErrorEmbedResource(context, botEx.Resource)
						.ToEmbed().QueueToChannel(context.Channel);
				}
				else
				{
					Log.Error(ex);
					await Global.ravenClient.CaptureAsync(new SentryEvent(ex));
				}
			};

			eventSystem.MessageFilter.AddFilter(new BotFilter());
			eventSystem.MessageFilter.AddFilter(new UserFilter());

			Global.Client.Attach(eventSystem);
			ConfigurationManager mg = new ConfigurationManager();

			var commandMap = new Framework.Events.CommandMap();
			commandMap.OnModuleLoaded += (module) =>
			{
				mg.RegisterType(module.GetReflectedInstance().GetType(), module.GetReflectedInstance());
			};

			var handler = new SimpleCommandHandler(pool, commandMap);

			handler.AddPrefix(">", true, true);
			handler.AddPrefix("miki.");

			var sessionHandler = new SessionBasedCommandHandler(pool);
			var messageHandler = new MessageListener(pool);

			eventSystem.AddCommandHandler(sessionHandler);
			eventSystem.AddCommandHandler(messageHandler);
			eventSystem.AddCommandHandler(handler);

			commandMap.RegisterAttributeCommands();
			commandMap.Install(eventSystem);

			string configFile = Environment.CurrentDirectory + Config.MikiConfigurationFile;

			if (File.Exists(configFile))
			{
				await mg.ImportAsync(
					new JsonSerializationProvider(),
					configFile
				);
			}

			await mg.ExportAsync(
				new JsonSerializationProvider(),
				configFile
			);

			if (!string.IsNullOrWhiteSpace(Global.Config.SharpRavenKey))
			{
				Global.ravenClient = new SharpRaven.RavenClient(Global.Config.SharpRavenKey);
			}

			handler.OnMessageProcessed += async (cmd, msg, time) =>
			{
				await Task.Yield();
				Log.Message($"{cmd.Name} processed in {time}ms");
			};

			Global.Client.Client.MessageCreate += Bot_MessageReceived;

			Global.Client.Client.GuildJoin += Client_JoinedGuild;
			Global.Client.Client.GuildLeave += Client_LeftGuild;
			Global.Client.Client.UserUpdate += Client_UserUpdated;

			await Global.Client.StartAsync();
		}

		private void InitLogging()
		{
			Log.OnLog += (msg, e) =>
			{
				if (e >= Global.Config.LogLevel)
				{
					Console.WriteLine(msg);
				}
			};

			Log.Theme.SetColor(LogLevel.Error, new LogColor { Foreground = ConsoleColor.Red });
			Log.Theme.SetColor(LogLevel.Warning, new LogColor { Foreground = ConsoleColor.Yellow });
		}

		private async Task Client_UserUpdated(IDiscordUser oldUser, IDiscordUser newUser)
		{
			if (oldUser.AvatarId != newUser.AvatarId)
			{
				await Utils.SyncAvatarAsync(newUser);
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
			IDiscordChannel defaultChannel = arg.GetDefaultChannel();

			//if (defaultChannel != null)
			//{
			//	LocaleInstance i = await Locale.GetLanguageInstanceAsync(defaultChannel.Id);
			//	defaultChannel.QueueMessageAsync(i.GetString("miki_join_message"));
			//}

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