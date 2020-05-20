using Miki.Configuration;
using Miki.Discord.Common;
using System.Threading.Tasks;
using Miki.Framework.Commands;

namespace Miki.Modules
{
    [Module("internal:servercount")]
	public class ServerCountModule
	{
		[Configurable]
		private string ConnectionString { get; set; } = "default";

        //public ServerCountModule(Module m, MikiApp b)
        //{
        //    m.JoinedGuild = OnUpdateGuilds;
        //    m.LeftGuild = OnUpdateGuilds;
        //    //	countLib = new CountLib(ConnectionString);
        //}

        private Task OnUpdateGuilds(IDiscordGuild g)
		{
			//MikiApp bot = MikiApp.Instance;

			//DiscordSocketClient client = bot.Client.GetShardFor(g);
			//await countLib.PostStats(client.ShardId, client.Guilds.Count);
			return Task.CompletedTask;
		}
	}
} 