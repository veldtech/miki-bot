using Microsoft.EntityFrameworkCore;
using Miki.Common;
using Miki.Configuration;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Commands;
using Miki.Framework.Events.Filters;
using Miki.Framework.Languages;
using Miki.Logging;
using Miki.Models;
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
        private static void Main(string[] args)
        {
			new Program().Start().GetAwaiter().GetResult();
		}

		public static Bot bot;
		public static DateTime timeSinceStartup;

		public async Task Start()
		{

			timeSinceStartup = DateTime.Now;

			Log.OnLog += (msg, e) => Console.WriteLine(msg);

			LogColor color = new LogColor();
			color.Foreground = ConsoleColor.Red;
			Log.Theme.SetColor(Logging.LogLevel.Error, color);

			LoadLocales();

			await LoadDiscord();

			for (int i = 0; i < Global.Config.MessageWorkerCount; i++)
				MessageBucket.AddWorker();

			using (var c = new MikiContext())
			{			
				List<User> bannedUsers = await c.Users.Where(x => x.Banned).ToListAsync();
				foreach(var u in bannedUsers)
				{
					bot.GetAttachedObject<EventSystem>().MessageFilter
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
			bot = new Bot(Global.Config.AmountShards, new ClientInformation()
            {
                Name = "Miki",
                Version = "0.6.2",
				ShardCount = Global.Config.ShardCount,
				DatabaseConnectionString = Global.Config.ConnString,
				Token = Global.Config.Token
			}, Global.RedisClient, Global.Config.RabbitUrl.ToString());
            
            EventSystem eventSystem = new EventSystem(new EventSystemConfig()
			{
				Developers = Global.Config.DeveloperIds,
				ErrorEmbedBuilder = new EmbedBuilder().SetTitle($"🚫 Something went wrong!").SetColor(new Color(1.0f, 0.0f, 0.0f))
			});

			bot.Attach(eventSystem);
			ConfigurationManager mg = new ConfigurationManager();

			var commandMap = new CommandMap();
			commandMap.OnModuleLoaded += (module) =>
			{
				mg.RegisterType(module.GetReflectedInstance());
			};
			commandMap.RegisterAttributeCommands();
			commandMap.Install(eventSystem, bot);

			var handler = new SimpleCommandHandler(commandMap);
			handler.AddPrefix(">", true);
			handler.AddPrefix("miki.");

			var sessionHandler = new SessionBasedCommandHandler();
			var messageHandler = new MessageListener();

			eventSystem.AddCommandHandler(sessionHandler);
			eventSystem.AddCommandHandler(messageHandler);
			eventSystem.AddCommandHandler(handler);

			//foreach(var x in mg.Containers)
			//{
			//	Console.WriteLine(x.Type.Name);
			//	foreach(var y in x.ConfigurableItems)
			//	{
			//		Console.WriteLine($"=> {y.Type.Name} : {y.Type.PropertyType} = {y.GetValue<object>().ToString()}");
			//	}
			//}

			//Console.WriteLine("---- loading config.json ----\nVALUES CHANGED TO:");

			////await mg.ImportAsync(new JsonSerializationProvider(), "./testexport.json");

			//foreach (var x in mg.Containers)
			//{
			//	Console.WriteLine(x.Type.Name);
			//	foreach (var y in x.ConfigurableItems)
			//	{
			//		Console.WriteLine($"=> {y.Type.Name} : {y.Type.PropertyType} = {y.GetValue<object>().ToString()}");
			//	}
			//}

			if (!string.IsNullOrWhiteSpace(Global.Config.SharpRavenKey))
            {
                Global.ravenClient = new SharpRaven.RavenClient(Global.Config.SharpRavenKey);
            }

			bot.Client.MessageCreate += Bot_MessageReceived;

			bot.Client.GuildJoin += Client_JoinedGuild;
			bot.Client.GuildLeave += Client_LeftGuild;
			bot.Client.UserUpdate += Client_UserUpdated;
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
			IDiscordChannel defaultChannel = await arg.GetDefaultChannelAsync();
			defaultChannel.QueueMessageAsync(Locale.GetString(defaultChannel.Id, "miki_join_message"));

			List<string> allArgs = new List<string>();
			List<object> allParams = new List<object>();
			List<object> allExpParams = new List<object>();

			try
			{
				var users = await arg.GetUsersAsync();
				for (int i = 0; i < users.Count; i++)
				{
					allArgs.Add($"(@p{i * 2}, @p{i * 2 + 1})");

					allParams.Add(users.ElementAt(i).Id.ToDbLong());
					allParams.Add(users.ElementAt(i).Username);

					allExpParams.Add((await users.ElementAt(i).GetGuildAsync()).Id.ToDbLong());
					allExpParams.Add(users.ElementAt(i).Id.ToDbLong());
				}

				using (var context = new MikiContext())
				{
					await context.Database.ExecuteSqlCommandAsync(
						$"INSERT INTO dbo.\"Users\" (\"Id\", \"Name\") VALUES {string.Join(",", allArgs)} ON CONFLICT DO NOTHING", allParams);
					await context.Database.ExecuteSqlCommandAsync(
						$"INSERT INTO dbo.\"LocalExperience\" (\"ServerId\", \"UserId\") VALUES {string.Join(",", allArgs)} ON CONFLICT DO NOTHING", allExpParams);
					await context.SaveChangesAsync();
				}
			}
			catch(Exception e)
			{
				Log.Error(e.ToString());
			}

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