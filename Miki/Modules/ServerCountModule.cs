using Discord;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Miki.Common.Interfaces;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Miki.Rest;
using Newtonsoft.Json;

namespace Miki.Modules
{
    [Module("internal:servercount")]
    internal class ServerCountModule
    {
		private class GuildCountObject
		{
			[JsonProperty("shard_id")]
			public int ShardId;

			[JsonProperty("shard_count")]
			public int ShardCount;

			[JsonProperty("server_count")]
			public int GuildCount;
		}

        public ServerCountModule(RuntimeModule m)
        {
            m.JoinedGuild = OnUpdateGuilds;
            m.LeftGuild = OnUpdateGuilds;
        }

        private async Task OnUpdateGuilds(IDiscordGuild g)
        {
            Bot bot = Bot.instance;

            await SendCarbon(bot);
            await SendDiscordBotsOrg(bot, g);
            await SendDiscordPW(bot, g);
        }

        private async Task SendCarbon(Bot bot)
        {
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                   { "key", Global.config.CarbonKey },
                   { "servercount", bot.Client.Guilds.Count.ToString() }
                };

                FormUrlEncodedContent content = new FormUrlEncodedContent(values);
                HttpResponseMessage response = await client.PostAsync("https://www.carbonitex.net/discord/data/botdata.php", content);
                string responseString = await response.Content.ReadAsStringAsync();
            }
        }

        private async Task SendDiscordBotsOrg(Bot bot, IDiscordGuild g)
        {
			var shard = bot.GetShardFor(g);
			var client = new RestClient("https://discordbots.org/api/bots/160105994217586689/stats");

			var guildCount = new GuildCountObject()
			{
				ShardId = shard.ShardId,
				ShardCount = bot.GetTotalShards(),
				GuildCount = shard.Guilds.Count
			};

			string json = JsonConvert.SerializeObject(guildCount);

			await client
				.SetAuthorization(Global.config.DiscordBotsOrgKey)
				.PostAsync<string>("", json);
        }

        private async Task SendDiscordPW(Bot bot, IDiscordGuild g)
        {
			var shard = bot.GetShardFor(g);
			var client = new RestClient("https://bots.discord.pw/api/bots/160105994217586689/stats");

			var guildCount = new GuildCountObject()
			{
				ShardId = shard.ShardId,
				ShardCount = bot.GetTotalShards(),
				GuildCount = shard.Guilds.Count
			};

			string json = JsonConvert.SerializeObject(guildCount);

			await client
				.SetAuthorization(Global.config.DiscordPwKey)
				.PostAsync<string>("", json);
        }
    }
}