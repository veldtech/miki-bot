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
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis;
using Miki.Modules;

namespace Miki
{
	public class Program
    {
        private static void Main(string[] args)
        {
			new Program().Start()
				.GetAwaiter()
				.GetResult();
        }

		public static Framework.Bot bot;
		public static DateTime timeSinceStartup;

		public async Task Start()
		{
			Locale.Load();
			timeSinceStartup = DateTime.Now;

			LoadDiscord();

			LevelNotificationsSetting s = await Setting.GetAsync<LevelNotificationsSetting>(0, DatabaseSettingId.LEVEL_NOTIFICATIONS);

			using (var c = new MikiContext())
			{			
				List<User> bannedUsers = await c.Users.Where(x => x.Banned).ToListAsync();
				foreach(var u in bannedUsers)
				{
					bot.Events.Ignore(u.Id.FromDbLong());
				}
			}

			await bot.ConnectAsync();
		}

        /// <summary>
        /// The program runs all discord services and loads all the data here.
        /// </summary>
        public void LoadDiscord()
        {
			Global.redisClient = new StackExchangeRedisCacheClient(new ProtobufSerializer(), Global.Config.RedisConnectionString);

			bot = new Bot(new ClientInformation()
            {
                Name = "Miki",
                Version = "0.5.4",
				cacheClient = Global.redisClient,
                Token = Global.Config.Token,
                ShardCount = Global.Config.ShardCount,
                ConsoleLogLevel = LogLevel.ALL,
				DatabaseConnectionString = Global.Config.ConnString
			});
		
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

			bot.Events.AddCommandDoneEvent(x =>
			{
				x.Name = "datadog-command-done";
				x.processEvent = async (msg, cmd, success, t) =>
				{
					if (!success)
					{
						DogStatsd.Counter("commands.error.rate", 1);
					}
					
					DogStatsd.Counter("commands.count", 1);
					DogStatsd.Histogram("commands.time", t, 0.1);
				};
			});

			bot.Events.RegisterPrefixInstance(">")
				.RegisterAsDefault();

			bot.Events.RegisterPrefixInstance("miki.", false);

			bot.MessageReceived += Bot_MessageReceived;
	
			bot.OnError = async (ex) => Log.Message(ex.ToString());
			bot.AddDeveloper(121919449996460033);

			foreach (ulong l in Global.Config.DeveloperIds)
			{
				bot.AddDeveloper(l);
			}

			bot.Client.JoinedGuild += Client_JoinedGuild;
			bot.Client.LeftGuild += Client_LeftGuild;

			bot.OnShardConnect += Bot_OnShardConnect;
			bot.OnShardDisconnect += Bot_OnShardDisconnect;
		}

		private async Task Bot_MessageReceived(Miki.Common.Interfaces.IDiscordMessage arg)
		{
			DogStatsd.Counter("messages.received", 1);
			await Task.Yield();
		}

		private async Task Client_LeftGuild(Discord.WebSocket.SocketGuild arg)
		{
			DogStatsd.Increment("guilds.left");
			DogStatsd.Counter("guilds", Bot.Instance.Client.Guilds.Count);
			await Task.Yield();
		}

		private async Task Client_JoinedGuild(IGuild arg)
		{
			Locale locale = new Locale(arg.Id);
			ITextChannel defaultChannel = await arg.GetDefaultChannelAsync();
			await defaultChannel.SendMessage(locale.GetString("miki_join_message"));

			// if miki patreon is present, leave again.

			DogStatsd.Increment("guilds.joined");
			DogStatsd.Counter("guilds", Bot.Instance.Client.Guilds.Count);
		}

		private async Task Bot_OnShardConnect(int shardId)
		{
			DogStatsd.Event("shard.connect", $"shard {shardId} has connected!");
			DogStatsd.ServiceCheck($"shard.{shardId}", Status.OK);
			await Task.Yield();
		}

		private async Task Bot_OnShardDisconnect(Exception e, int shardId)
		{
			DogStatsd.Event("shard.disconnect", $"shard {shardId} has disconnected!");
			DogStatsd.ServiceCheck($"shard.{shardId}", Status.CRITICAL, null, null, null, e.Message);
			await Task.Yield();
		}
	}
}