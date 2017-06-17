using Meru;
using Meru.Events;
using Meru.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    class ServerCountModule
    {
        public async Task LoadEvents(Client bot)
        {
            await new RuntimeModule("--servercount")
            {
                JoinedGuild = (g) => OnUpdateGuilds(bot, g),
                LeftGuild = (g) => OnUpdateGuilds(bot, g)
            }.InstallAsync(bot);
        }

        private async Task OnUpdateGuilds(Client bot, IDiscordGuild g)
        {
            await SendCarbon(bot);
            await SendDiscordBotsOrg(bot, g);
            await SendDiscordPW(bot, g);
        }

        private async Task SendCarbon(Client bot)
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
        private async Task SendDiscordBotsOrg(Client bot, IDiscordGuild g)
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
        private async Task SendDiscordPW(Client bot, IDiscordGuild g)
        {
            using (var client = new HttpClient())
            {
                using (var content = new StringContent($"{{ \"server_count\": {bot.Client.Guilds.Count}}}", Encoding.UTF8, "application/json"))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Global.DiscordPwKey);
                    HttpResponseMessage response = await client.PostAsync("https://discordbots.org/api/bots/160105994217586689/stats", content);
                }
            }
        }
    }
}