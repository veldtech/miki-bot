using Miki.Discord.Common;
using Miki.Discord.Common.Utils;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Utility
{
    public static class SearchUtils
    {
        public static async Task<IDiscordUser> FindUserAsync(this IDiscordGuild guild, string userName)
        {
            if (ulong.TryParse(userName, out var userId))
            {
                return await guild.GetMemberAsync(userId);
            }

            if (Mention.TryParse(userName, out Mention m))
            {
                if (m.Type == MentionType.USER
                    || m.Type == MentionType.USER_NICKNAME)
                {
                    return await guild.GetMemberAsync(m.Id);
                }
                return null;
            }

            var members = await guild.GetMembersAsync();
            return members.FirstOrDefault(x => (x.Nickname?.ToLowerInvariant() ?? null) == userName.ToLowerInvariant()
                || x.Username.ToLowerInvariant() == userName.ToLowerInvariant());
        }
    }
}
