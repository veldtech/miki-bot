using Miki.Discord.Common;

namespace Miki
{
    public static class DiscordExtensions
    {
        public static string GetFullName(this IDiscordUser user)
        {
            return $"{user.Username}#{user.Discriminator}";
        }
    }
}
