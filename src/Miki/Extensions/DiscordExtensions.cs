using Miki.Discord;
using Miki.Discord.Common;

namespace Miki
{
    public static class DiscordExtensions
    {
        public static string GetFullName(this IDiscordUser user)
        {
            return $"{user.Username}#{user.Discriminator}";
        }

        public static EmbedBuilder AddCodeBlock(this EmbedBuilder builder, object str, string language = null)
        {
            return builder.SetDescription(builder.ToEmbed().Description + $"```{language}\n{str}\n```");
        }
    }
}
