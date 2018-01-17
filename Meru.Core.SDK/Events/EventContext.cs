using IA.SDK.Interfaces;
using System.Threading.Tasks;

namespace IA.SDK.Events
{
    public class EventContext
    {
        public string arguments;

        // public IBot bot;
        public ICommandHandler commandHandler;

        public IDiscordMessage message;

        public IDiscordUser Author => message.Author;
        public async Task<IDiscordUser> GetCurrentUserAsync() => await Guild.GetCurrentUserAsync();

        public IDiscordMessageChannel Channel => message.Channel;
        public IDiscordGuild Guild => message.Guild;
    }
}