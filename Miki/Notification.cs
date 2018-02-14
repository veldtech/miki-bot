using Discord;
using Miki.Framework;
using Miki.Common;
using Miki.Common.Interfaces;
using Miki.Accounts.Achievements;
using Miki.Models;
using System.Threading.Tasks;

namespace Miki
{
    internal class Notification
    {
        public static async Task SendAchievement(AchievementDataContainer d, int rank, IDiscordMessageChannel channel, IDiscordUser user)
        {
            await SendAchievement(d.Achievements[rank], channel, user);
        }

        public static async Task SendAchievement(BaseAchievement d, IDiscordMessageChannel channel, IDiscordUser user)
        {
			IDiscordEmbed embed = Utils.Embed.SetTitle("Achievement Unlocked")
				.SetDescription($"{d.Icon} **{user.Username}#{user.Discriminator}** has unlocked the achievement **{d.Name}**! {d.Icon}");

			embed.QueueToChannel(channel);
		}
    }
}