using Discord;
using Miki.Framework;
using Miki.Framework.FileHandling;
using Miki.Languages;
using Miki.Models;
using Newtonsoft.Json;
using StatsdClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Protobuf;
using Miki.Common;
using Miki.Framework.Events;
using StackExchange.Redis;
using System.Threading;
using Discord.WebSocket;
using Miki.Framework.Extension;
using Amazon.S3.Model;
using Miki.Framework.Languages;
using System.Reflection;
using System.Resources;
using Microsoft.Extensions.Logging;

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
			Log.Theme.SetColor(LogLevel.Error, color);

			LoadLocales();

			LoadDiscord();

			for (int i = 0; i < Global.Config.MessageWorkerCount; i++)
				MessageBucket.AddWorker();

			using (var c = new MikiContext())
			{			
				List<User> bannedUsers = await c.Users.Where(x => x.Banned).ToListAsync();
				foreach(var u in bannedUsers)
				{
					EventSystem.Instance.Ignore(u.Id.FromDbLong());
				}
			}

			await bot.ConnectAsync(Global.Config.Token);
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

		public void LoadDiscord()
        {
			if (!Global.Config.IsMainBot)
			{
				WebhookManager.Listen("webhook");

				WebhookManager.OnEvent += async (eventArgs) =>
				{
					Console.WriteLine("[webhook] " + eventArgs.auth_code);
				};
			}

			bot = new Bot(Global.Config.AmountShards, new DiscordSocketConfig()
			{
				ShardId = Global.Config.ShardId,
				TotalShards = Global.Config.ShardCount,
				ConnectionTimeout = 100000,
				LargeThreshold = 250,
			}, new ClientInformation()
            {
                Name = "Miki",
                Version = "0.6.1",
				ShardCount = Global.Config.ShardCount,
				DatabaseConnectionString = Global.Config.ConnString,
			});

			var eventSystem = EventSystem.Start(bot);

            if (!string.IsNullOrWhiteSpace(Global.Config.SharpRavenKey))
            {
                Global.ravenClient = new SharpRaven.RavenClient(Global.Config.SharpRavenKey);
            }

			if(!string.IsNullOrWhiteSpace(Global.Config.DatadogKey))
			{
				var dogstatsdConfig = new StatsdConfig
				{
					StatsdServerName = Global.Config.DatadogHost,
					StatsdPort = 8125,
					Prefix = "miki"
				};
				DogStatsd.Configure(dogstatsdConfig);
			}

			eventSystem.AddCommandDoneEvent(x =>
			{
				x.Name = "datadog-command-done";
				x.processEvent = async (msg, cmd, success, t) =>
				{
					if (!success)
					{
						DogStatsd.Counter("commands.error.rate", 1);
					}

					if (cmd.Module == null)
						return;

					DogStatsd.Histogram("commands.time", t, 0.1, new[]
					{ $"commandtype:{cmd.Module.Name.ToLowerInvariant()}", $"commandname:{cmd.Name.ToLowerInvariant()}" });
					DogStatsd.Counter("commands.count", 1, 1, new[]
					{ $"commandtype:{cmd.Module.Name.ToLowerInvariant()}", $"commandname:{cmd.Name.ToLowerInvariant()}" });
				};
			});

			eventSystem.RegisterPrefixInstance(">").RegisterAsDefault();
			eventSystem.RegisterPrefixInstance("miki.", false);

			bot.Client.MessageReceived += Bot_MessageReceived;
	
			eventSystem.AddDeveloper(121919449996460033);

			foreach (ulong l in Global.Config.DeveloperIds)
			{
				eventSystem.AddDeveloper(l);
			}

			bot.Client.JoinedGuild += Client_JoinedGuild;
			bot.Client.LeftGuild += Client_LeftGuild;
			bot.Client.UserUpdated += Client_UserUpdated;

			bot.Client.ShardConnected += Bot_OnShardConnect;
			bot.Client.ShardDisconnected += Bot_OnShardDisconnect;
		}

		private async Task Client_UserUpdated(SocketUser oldUser, SocketUser newUser)
		{
			if (oldUser.AvatarId != newUser.AvatarId)
			{
				await Utils.SyncAvatarAsync(newUser);
			}
		}

		private async Task Bot_MessageReceived(IMessage arg)
		{
			DogStatsd.Increment("messages.received");
		}

		private async Task Client_LeftGuild(SocketGuild arg)
		{
			DogStatsd.Increment("guilds.left");
			DogStatsd.Set("guilds", Bot.Instance.Client.Guilds.Count, Bot.Instance.Client.Guilds.Count);
			await Task.Yield();
		}

		private async Task Client_JoinedGuild(IGuild arg)
		{
			ITextChannel defaultChannel = await arg.GetDefaultChannelAsync();
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

					allExpParams.Add(users.ElementAt(i).GuildId.ToDbLong());
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
			DogStatsd.Set("guilds", Bot.Instance.Client.Guilds.Count, Bot.Instance.Client.Guilds.Count);
		}

		private async Task Bot_OnShardConnect(DiscordSocketClient client)
		{
			DogStatsd.Event("shard.connect", $"shard {client.ShardId} has connected!");
			DogStatsd.ServiceCheck($"shard.up", Status.OK, null, $"miki.shard.{client.ShardId}");
			await Task.Yield();
		}

		private async Task Bot_OnShardDisconnect(Exception e, DiscordSocketClient client)
		{
			DogStatsd.Event("shard.disconnect", $"shard {client.ShardId} has disconnected!");
			DogStatsd.ServiceCheck($"shard.up", Status.CRITICAL, null, $"miki.shard.{client.ShardId}", null, e.Message);
			await Task.Yield();
		}
	}
}