using Microsoft.EntityFrameworkCore;
using Miki.Cache;
using Miki.Cache.Serializers.Protobuf;
using Miki.Cache.StackExchange;
using Miki.Common;
using Miki.Configuration;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Internal;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Commands;
using Miki.Framework.Events.Filters;
using Miki.Framework.Languages;
using Miki.Logging;
using Miki.Models;
using StackExchange.Redis;
using StatsdClient;
using System;
using System.Collections.Generic;
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
				Global.MikiApi = new API.MikiApi(Global.Config.MikiApiBaseUrl, Global.Config.MikiApiKey);
			}

			for (int i = 0; i < Global.Config.MessageWorkerCount; i++)
			{
				MessageBucket.AddWorker();
			}

			using (var c = new MikiContext())
			{			
				List<User> bannedUsers = await c.Users.Where(x => x.Banned).ToListAsync();
				foreach(var u in bannedUsers)
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

			Global.Client = new Bot(Global.Config.AmountShards, pool, new ClientInformation()
            {
                Name = "Miki",
                Version = "0.6.2",
				ShardCount = Global.Config.ShardCount,
				DatabaseConnectionString = Global.Config.ConnString,
				Token = Global.Config.Token
			}, Global.Config.RabbitUrl.ToString());
            
            EventSystem eventSystem = new EventSystem(new EventSystemConfig()
			{
				Developers = Global.Config.DeveloperIds,
				ErrorEmbedBuilder = new EmbedBuilder()
					.SetTitle($"🚫 Something went wrong!")
					.SetColor(new Color(1.0f, 0.0f, 0.0f))
			});

			eventSystem.MessageFilter.AddFilter(new BotFilter());
			eventSystem.MessageFilter.AddFilter(new UserFilter());

			Global.Client.Attach(eventSystem);
			ConfigurationManager mg = new ConfigurationManager();

			var commandMap = new Framework.Events.CommandMap();
			commandMap.OnModuleLoaded += (module) =>
			{
				mg.RegisterType(module.GetReflectedInstance());
			};

			var handler = new SimpleCommandHandler(commandMap);

			handler.AddPrefix(">", true, true);
			handler.AddPrefix("miki.");

			var sessionHandler = new SessionBasedCommandHandler();
			var messageHandler = new MessageListener();

			eventSystem.AddCommandHandler(sessionHandler);
			eventSystem.AddCommandHandler(messageHandler);
			eventSystem.AddCommandHandler(handler);

			commandMap.RegisterAttributeCommands();
			commandMap.Install(eventSystem, Global.Client);

			if (!string.IsNullOrWhiteSpace(Global.Config.SharpRavenKey))
            {
                Global.ravenClient = new SharpRaven.RavenClient(Global.Config.SharpRavenKey);
            }

			handler.OnMessageProcessed += async (cmd, msg, time) =>
			{
				await Task.Yield();
				Log.Message($"{cmd.Name} processed in {time}ms");
			};

			Global.Client.Client.MessageCreate += Bot_MessageReceived;;

			Global.Client.Client.GuildJoin += Client_JoinedGuild;
			Global.Client.Client.GuildLeave += Client_LeftGuild;
			Global.Client.Client.UserUpdate += Client_UserUpdated;
		}

		private void InitLogging()
		{
			Log.OnLog += (msg, e) =>
			{
				if (e >= LogLevel.Information)
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

		private async Task Client_LeftGuild(ulong guildId)
		{
			DogStatsd.Increment("guilds.left");
			await Task.Yield();
		}

		private async Task Client_JoinedGuild(IDiscordGuild arg)
		{
			//IDiscordChannel defaultChannel = await arg.GetDefaultChannelAsync();

			//if (defaultChannel != null)
			//{
			//	defaultChannel.QueueMessageAsync(Locale.GetString(defaultChannel.Id, "miki_join_message"));
			//}

			List<string> allArgs = new List<string>();
			List<object> allParams = new List<object>();
			List<object> allExpParams = new List<object>();

			//try
			//{
			//	var users = await arg.GetUsersAsync();
			//	for (int i = 0; i < users.Count; i++)
			//	{
			//		allArgs.Add($"(@p{i * 2}, @p{i * 2 + 1})");

			//		allParams.Add(users.ElementAt(i).Id.ToDbLong());
			//		allParams.Add(users.ElementAt(i).Username);

			//		allExpParams.Add((await users.ElementAt(i).GetGuildAsync()).Id.ToDbLong());
			//		allExpParams.Add(users.ElementAt(i).Id.ToDbLong());
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
			//catch(Exception e)
			//{
			//	Log.Error(e.ToString());
			//}

			DogStatsd.Increment("guilds.joined");
		//	DogStatsd.Set("guilds", Bot.Instance.Client.Guilds.Count, Bot.Instance.Client.Guilds.Count);
		}

		//private async Task Bot_OnShardConnect(DiscordSocketClient client)
		//{
		//	Log.Message($"shard {client.ShardId} has connected as {client.CurrentUser.ToString()}!");
		//	DogStatsd.Event("shard.connect", $"shard {client.ShardId} has connected!");
		//	DogStatsd.ServiceCheck($"shard.up", Status.OK, null, $"miki.shard.{client.ShardId}");
		//	await Task.Yield();
		//}

		//private async Task Bot_OnShardDisconnect(Exception e, DiscordSocketClient client)
		//{
		//	Log.Error($"Shard {client.ShardId} has disconnected!");
		//	DogStatsd.Event("shard.disconnect", $"shard {client.ShardId} has disconnected!\n" + e.ToString());
		//	DogStatsd.ServiceCheck($"shard.up", Status.CRITICAL, null, $"miki.shard.{client.ShardId}", null, e.Message);
		//	await Task.Yield();
		//}
	}
}