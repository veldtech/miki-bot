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
        public static void SendAchievement(AchievementDataContainer d, int rank, IDiscordMessageChannel channel, IDiscordUser user)
			=> SendAchievement(d.Achievements[rank], channel, user);

		public static void SendAchievement(BaseAchievement d, IDiscordMessageChannel channel, IDiscordUser user)
        {
			IDiscordEmbed embed = Utils.Embed.SetTitle("Achievement Unlocked")
				.SetDescription($"{d.Icon} **{user.Username}#{user.Discriminator}** has unlocked the achievement **{d.Name}**! {d.Icon}");

			embed.QueueToChannel(channel);
		}
    }
}