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
using Discord.WebSocket;

namespace Miki.Modules
{
    [Module("internal:servercount")]
    internal class ServerCountModule
    {
        public ServerCountModule(Module m)
        {
            m.JoinedGuild = OnUpdateGuilds;
            m.LeftGuild = OnUpdateGuilds;
        }

        private async Task OnUpdateGuilds(IGuild g)
        {
			Bot bot = Bot.Instance;
			DiscordSocketClient client = bot.Client.GetShardFor(g);
			

        }
    }
}