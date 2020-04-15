namespace Miki.Utility
{
    using System.Linq;
    using System.Threading.Tasks;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Exceptions;
    using Miki.Discord.Common;
    using Miki.Framework;
    using Miki.Modules.Admin.Exceptions;

    public static class SearchUtils
    {
        public static Task<IDiscordRole> FindRoleAsync(
            this IDiscordGuild guild, IContext context)
        {
            if(context.GetArgumentPack().Take(out string resource))
            {
                return FindRoleByIdAsync(guild, resource)
                    .OrElse(() => FindRoleByMentionAsync(guild, resource))
                    .OrElse(() => FindRoleByNameAsync(guild, resource));
            }

            throw new InvalidEntityException("role");
        }

        public static Task<IDiscordGuildChannel> FindChannelAsync(
            this IDiscordGuild guild, IContext context)
        {
            if(context.GetArgumentPack().Take(out string resource))
            {
                return FindChannelByIdAsync(guild, resource)
                    .OrElse(() => FindChannelByMentionAsync(guild, resource))
                    .OrElse(() => FindChannelByNameAsync(guild, resource));
            }

            throw new InvalidEntityException("channel");
        }

        public static Task<IDiscordGuildUser> FindUserAsync(this IDiscordGuild guild, string argument)
        {
            return FindUserByIdAsync(guild, argument)
                .OrElse(() => FindUserByMentionAsync(guild, argument))
                .OrElse(() => FindUserByNameAsync(guild, argument))
                .OrElseThrow(new EntityNullException<User>());
        }
        public static Task<IDiscordGuildUser> FindUserAsync(this IDiscordGuild guild, IContext context)
        {
            if(context.GetArgumentPack().Take(out string resource))
            {
                return guild.FindUserAsync(resource);
            }

            throw new InvalidEntityException("user");
        }

        public static Task<IDiscordRole> FindRoleByIdAsync(IDiscordGuild guild, string id)
        {
            if(ulong.TryParse(id, out var roleId))
            {
                return guild.GetRoleAsync(roleId);
            }

            return Task.FromException<IDiscordRole>(
                new InvalidEntityException("role"));
        }

        public static Task<IDiscordRole> FindRoleByMentionAsync(IDiscordGuild guild, string id)
        {
            if(Mention.TryParse(id, out Mention m))
            {
                if(m.Type == MentionType.ROLE)
                {
                    return guild.GetRoleAsync(m.Id);
                }
            }
            return Task.FromException<IDiscordRole>(new InvalidEntityException("role"));
        }

        public static Task<IDiscordRole> FindRoleByNameAsync(IDiscordGuild guild, string id)
        {
            return guild.GetRolesAsync()
                .Map(y => y.FirstOrDefault(x => x.Name.ToLowerInvariant() == id));
        }

        public static async Task<IDiscordGuildChannel> FindChannelByIdAsync(IDiscordGuild guild, string id)
        {
            if(ulong.TryParse(id, out var channelId))
            {
                return await guild.GetChannelAsync(channelId);
            }

            throw new InvalidEntityException("id");
        }

        public static Task<IDiscordGuildChannel> FindChannelByMentionAsync(IDiscordGuild guild, string id)
        {
            if(Mention.TryParse(id, out Mention m))
            {
                if(m.Type == MentionType.CHANNEL)
                {
                    return guild.GetChannelAsync(m.Id);
                }
            }
            return Task.FromException<IDiscordGuildChannel>(new InvalidEntityException("id"));
        }

        public static Task<IDiscordGuildChannel> FindChannelByNameAsync(IDiscordGuild guild, string id)
            => guild.GetChannelsAsync()
                .Map(y => y.FirstOrDefault(x => x.Name.ToLowerInvariant() == id));

        public static async Task<IDiscordGuildUser> FindUserByIdAsync(IDiscordGuild guild, string id)
        {
            if(ulong.TryParse(id, out ulong userId))
            {
                return await guild.GetMemberAsync(userId);
            }
            throw new InvalidEntityException("id");
        }

        public static async Task<IDiscordGuildUser> FindUserByMentionAsync(IDiscordGuild guild, string id)
        {
            if(!Mention.TryParse(id, out var mention))
            {
                throw new InvalidEntityException("user");
            }

            if(mention.Type == MentionType.USER
               || mention.Type == MentionType.USER_NICKNAME)
            {
                return await guild.GetMemberAsync(mention.Id);
            }
            throw new InvalidEntityException("user");
        }

        public static Task<IDiscordGuildUser> FindUserByNameAsync(IDiscordGuild guild, string name)
            => guild.GetMembersAsync()
                .Map(r => r.First(
                    x => x.Nickname?.ToLowerInvariant() == name.ToLowerInvariant() 
                         || x.Username.ToLowerInvariant() == name.ToLowerInvariant()));
    }
}
    