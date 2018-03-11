using Discord;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Miki.Rest;
using Newtonsoft.Json;
using DiscordBotsList.Api;

namespace Miki.Modules
{
    [Module("internal:servercount")]
    internal class ServerCountModule
    {
		AuthDiscordBotListApi api;

		private class GuildCountObject
		{
			[JsonProperty("shard_id")]
			public int ShardId;

			[JsonProperty("shard_count")]
			public int ShardCount;

			[JsonProperty("server_count")]
			public int GuildCount;
		}

        public ServerCountModule(Module m)
        {
            m.JoinedGuild = OnUpdateGuilds;
            m.LeftGuild = OnUpdateGuilds;
        }

        private async Task OnUpdateGuilds(IGuild g)
        {
            Bot bot = Bot.Instance as Bot;

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
                   { "key", Global.Config.CarbonKey },
                   { "servercount", bot.Client.Guilds.Count.ToString() }
                };

                FormUrlEncodedContent content = new FormUrlEncodedContent(values);
                HttpResponseMessage response = await client.PostAsync("https://www.carbonitex.net/discord/data/botdata.php", content);
                string responseString = await response.Content.ReadAsStringAsync();
            }
        }

        private async Task SendDiscordBotsOrg(Bot bot, IGuild g)
        {
			//var shard = bot.GetShardFor(g);

			//if (api == null)
			//	api = new AuthDiscordBotListApi(shard.CurrentUser.Id, Global.Config.DiscordBotsOrgKey);

			//await api.UpdateStats(shard.ShardId, bot.Information.ShardCount, new[] { shard.Guilds.Count });
        }

        private async Task SendDiscordPW(Bot bot, IGuild g)
        {
			var shard = bot.Client.GetShardFor(g);
			var client = new RestClient("https://bots.discord.pw/api/bots/160105994217586689/stats");

			var guildCount = new GuildCountObject()
			{
				ShardId = shard.ShardId,
				ShardCount = bot.Information.ShardCount,
				GuildCount = shard.Guilds.Count
			};

			string json = JsonConvert.SerializeObject(guildCount);

			await client
				.SetAuthorization(Global.Config.DiscordPwKey)
				.PostAsync<string>("", json);
        }
    }
}