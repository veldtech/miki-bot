	using Miki.Accounts.Achievements;
using Miki.Discord;
using Miki.Discord.Common;
using System.Threading.Tasks;

namespace Miki
{
	internal class Notification
	{
		public static void SendAchievement(AchievementDataContainer d, int rank, IDiscordChannel channel, IDiscordUser user)
		{
			SendAchievement(d.Achievements[rank], channel, user);
		}

		public static void SendAchievement(IAchievement d, IDiscordChannel channel, IDiscordUser user)
		{
			CreateAchievementEmbed(d, user).QueueToChannel(channel);
		}

		public static async Task SendAchievementAsync(IAchievement baseAchievement, IDiscordUser user)
		{
			SendAchievement(baseAchievement, await user.GetDMChannelAsync(), user);
		}

		private static DiscordEmbed CreateAchievementEmbed(IAchievement baseAchievement, IDiscordUser user)
		{
			return new EmbedBuilder()
				.SetTitle("Achievement Unlocked")
				.SetDescription($"{baseAchievement.Icon} **{user.Username}#{user.Discriminator}** has unlocked the achievement **{baseAchievement.Name}**! {baseAchievement.Icon}").ToEmbed();
		}
	}
}