using Miki.Framework;
using Miki.Common;
using Miki.Accounts.Achievements;
using Miki.Models;
using System.Threading.Tasks;
using System;
using Miki.Framework.Extension;
using Miki.Discord.Common;
using Miki.Discord;

namespace Miki
{
	internal class Notification
	{
		public static void SendAchievement(AchievementDataContainer d, int rank, IDiscordChannel channel, IDiscordUser user)
		{
			SendAchievement(d.Achievements[rank], channel, user);
		}
		public static void SendAchievement(BaseAchievement d, IDiscordChannel channel, IDiscordUser user)
		{
			CreateAchievementEmbed(d, user).QueueToChannel(channel);	
		}
		public static async Task SendAchievementAsync(BaseAchievement baseAchievement, IDiscordUser user)
		{
			SendAchievement(baseAchievement, await user.GetDMChannelAsync(), user);
		}

		private static DiscordEmbed CreateAchievementEmbed(BaseAchievement baseAchievement, IDiscordUser user)
		{
			return Utils.Embed.SetTitle("Achievement Unlocked")
				.SetDescription($"{baseAchievement.Icon} **{user.Username}#{user.Discriminator}** has unlocked the achievement **{baseAchievement.Name}**! {baseAchievement.Icon}").ToEmbed();
		}
	}
}