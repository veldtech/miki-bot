namespace Miki
{
    using Miki.Discord.Common;

    public static class DiscordExtensions
    {
        public static string GetFullName(this IDiscordUser user)
        {
            return $"{user.Username}#{user.Discriminator}";
        }
    }
}
