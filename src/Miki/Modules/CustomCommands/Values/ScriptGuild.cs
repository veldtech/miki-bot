using System.Threading.Tasks;
using Miki.Discord.Common;
using MiScript.Attributes;

namespace Miki.Modules.CustomCommands.Values
{
    public class ScriptGuild
    {
        private readonly IDiscordGuild guild;
        private ScriptUser owner;

        public ScriptGuild(IDiscordGuild guild)
        {
            this.guild = guild;
        }

        [Property("id")]
        public ulong Id => guild.Id;

        [Property("members")]
        public int MemberCount => guild.MemberCount;

        [Property("icon")]
        public string Icon => guild.IconUrl;

        [Function("get_owner")]
        public async Task<ScriptUser> GetOwnerAsync()
        {
            return owner ??= new ScriptUser(await guild.GetOwnerAsync());
        }

        public override string ToString()
        {
            return guild.Name;
        }
    }
}