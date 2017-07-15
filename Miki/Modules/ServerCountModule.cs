using Discord;
using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module("internal:servercount")]
    class ServerCountModule
    {
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
                   { "key", Global.CarbonitexKey },
                   { "servercount", bot.Client.Guilds.Count.ToString() }
                };
                FormUrlEncodedContent content = new FormUrlEncodedContent(values);
                HttpResponseMessage response = await client.PostAsync("https://www.carbonitex.net/discord/data/botdata.php", content);
                string responseString = await response.Content.ReadAsStringAsync();
            }
        }
        private async Task SendDiscordBotsOrg(Bot bot, IDiscordGuild g)
        {
            using (var client = new HttpClient())
            {
                using (var content = new StringContent($"{{ \"server_count\": {bot.Client.Guilds.Count}}}", Encoding.UTF8, "application/json"))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Global.DiscordBotsOrgKey);
                    HttpResponseMessage response = await client.PostAsync("https://discordbots.org/api/bots/160105994217586689/stats", content);
                }
            }
        }
        private async Task SendDiscordPW(Bot bot, IDiscordGuild g)
        {
            using (var client = new HttpClient())
            {
                using (var content = new StringContent("{\"shard_id\": " + Bot.instance.Client.GetShardIdFor((g as IProxy<IGuild>).ToNativeObject()) + ", \"shard_count\": " + Bot.instance.Client.Shards.Count + ", \"server_count\": " + Bot.instance.Client.GetShardFor((g as IProxy<IGuild>).ToNativeObject()).Guilds.Count + "}", Encoding.UTF8, "application/json"))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Global.DiscordPwKey);
                    HttpResponseMessage response = await client.PostAsync("https://bots.discord.pw/api/bots/160105994217586689/stats", content);
                }
            }
        }
    }
} 