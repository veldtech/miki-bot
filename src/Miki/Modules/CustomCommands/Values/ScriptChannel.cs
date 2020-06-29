using Miki.Discord.Common;
using MiScript.Attributes;

namespace Miki.Modules.CustomCommands.Values
{
    public class ScriptChannel
    {
        private readonly IDiscordChannel channel;

        public ScriptChannel(IDiscordChannel channel)
        {
            this.channel = channel;
        }

        [Property("id")]
        public ulong Id => channel.Id;
        
        [Property("name")]
        public string Name => channel.Name;
        
        [Property("nsfw")]
        public bool IsNsfw => channel.IsNsfw;

        public override string ToString()
        {
            return channel.Name;
        }
    }
}