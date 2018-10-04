using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using System.Threading.Tasks;
using CountAPI;
using Miki.Configuration;
using Miki.Discord.Common;

namespace Miki.Modules
{
	[Module("internal:servercount")]
	public class ServerCountModule
	{
		[Configurable]
		private string ConnectionString { get; set; } = "default";

		private readonly CountLib _countLib;

		public ServerCountModule(Module m, Framework.Bot b)
		{
			m.JoinedGuild = OnUpdateGuilds;
			m.LeftGuild = OnUpdateGuilds;
		//	countLib = new CountLib(ConnectionString);
		}

		private async Task OnUpdateGuilds(IDiscordGuild g)
		{
			Framework.Bot bot = Framework.Bot.Instance;

			//DiscordSocketClient client = bot.Client.GetShardFor(g);
			//await countLib.PostStats(client.ShardId, client.Guilds.Count);
		}
	}
}