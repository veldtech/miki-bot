using Miki.Discord.Common;
using Miki.Discord.Common.Utils;
using Miki.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Utility
{
    public static class SearchUtils
    {
        public static async Task<IDiscordRole> FindRoleAsync(
            this IDiscordGuild guild,
            string resource)
        {
            if(ulong.TryParse(resource, out var roleId))
            {
                return await guild.GetRoleAsync(roleId);
            }

            if(Mention.TryParse(resource, out Mention m))
            {
                if(m.Type == MentionType.ROLE)
                {
                    return await guild.GetRoleAsync(m.Id);
                }
                return null;
            }

            var channels = await guild.GetRolesAsync();
            return channels.FirstOrDefault(x => x.Name.ToLowerInvariant() == resource);
        }

        public static async Task<IDiscordChannel> FindChannelAsync(
            this IDiscordGuild guild,
            string resource)
        {
            if (ulong.TryParse(resource, out var channelId))
            {
                return await guild.GetChannelAsync(channelId);
            }

            if (Mention.TryParse(resource, out Mention m))
            {
                if (m.Type == MentionType.CHANNEL)
                {
                    return await guild.GetChannelAsync(m.Id);
                }
                return null;
            }

            var channels = await guild.GetChannelsAsync();
            return channels.FirstOrDefault(x => x.Name.ToLowerInvariant() == resource);
        }

        public static async Task<IDiscordUser> FindUserAsync(
            this IDiscordGuild guild, 
            string userName)
        {
            if(ulong.TryParse(userName, out var userId))
            {
                return await guild.GetMemberAsync(userId);
            }
            
            if(Mention.TryParse(userName, out Mention m))
            {
                if(m.Type == MentionType.USER
                    || m.Type == MentionType.USER_NICKNAME)
                {
                    return await guild.GetMemberAsync(m.Id);
                }
                return null;
            }

            var members = await guild.GetMembersAsync();
            return members.FirstOrDefault(
                x => x.Nickname?.ToLowerInvariant() == userName.ToLowerInvariant() 
                || x.Username.ToLowerInvariant() == userName.ToLowerInvariant());
        }
    }
}
