using Miki.Discord.Common;
using MiScript.Attributes;

namespace Miki.Modules.CustomCommands.Values
{
    public class ScriptUser
    {
        private readonly IDiscordUser user;

        public ScriptUser(IDiscordUser user)
        {
            this.user = user;
        }

        [Property("id")]
        public ulong Id => user.Id;

        [Property("bot")]
        public bool Bot => user.IsBot;

        [Property("mention")]
        public string Mention => user.Mention;
        
        [Property("discrim")]
        public string Discriminator => user.Discriminator.ToString();
        
        [Property("name")]
        public string Name => user.Username;

        public override string ToString()
        {
            return $"{user.Username}#{user.Discriminator}";
        }
    }
}