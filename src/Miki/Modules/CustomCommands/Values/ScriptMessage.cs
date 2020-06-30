using Miki.Discord.Common;
using MiScript.Attributes;

namespace Miki.Modules.CustomCommands.Values
{
    public class ScriptMessage
    {
        private readonly IDiscordMessage message;

        public ScriptMessage(IDiscordMessage message)
        {
            this.message = message;
        }

        [Property("id")]
        public ulong Id => message.Id;
        
        [Property("content")]
        public string Content => message.Content;

        public override string ToString()
        {
            return message.Content;
        }
    }
}