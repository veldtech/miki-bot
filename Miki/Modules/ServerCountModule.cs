using Discord;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using System.Threading.Tasks;
using Discord.WebSocket;
using CountAPI;

namespace Miki.Modules
{
	[Module("internal:servercount")]
	internal class ServerCountModule
	{
		private CountLib countLib;

		public ServerCountModule(Module m, Bot b)
		{
			m.JoinedGuild = OnUpdateGuilds;
			m.LeftGuild = OnUpdateGuilds;
			countLib = new CountLib();
		}

		private async Task OnUpdateGuilds(IGuild g)
		{
			Bot bot = Bot.Instance;
			DiscordSocketClient client = bot.Client.GetShardFor(g);
			await countLib.PostStats(client.ShardId, client.Guilds.Count);
		}
	}
}