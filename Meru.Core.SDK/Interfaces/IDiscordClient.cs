using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.SDK.Interfaces
{
    public interface IDiscordClient
    {
        int ShardId { get; }

        List<IDiscordGuild> Guilds { get; }

        IDiscordUser GetUser(ulong id);

        Task SetGameAsync(string game, string link = "");
    }
}