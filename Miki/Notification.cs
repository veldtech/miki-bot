using Discord;
using Miki.Framework;
using Miki.Common;
using Miki.Accounts.Achievements;
using Miki.Models;
using System.Threading.Tasks;
using System;
using Miki.Framework.Extension;

namespace Miki
{
	internal class Notification
	{
		public static void SendAchievement(AchievementDataContainer d, int rank, IMessageChannel channel, IUser user)
		{
			SendAchievement(d.Achievements[rank], channel, user);
		}
		public static void SendAchievement(BaseAchievement d, IMessageChannel channel, IUser user)
		{
			CreateAchievementEmbed(d, user).QueueToChannel(channel);	
		}
		public static async Task SendAchievementAsync(BaseAchievement baseAchievement, IUser user)
		{
			SendAchievement(baseAchievement, await user.GetOrCreateDMChannelAsync(), user);
		}

		private static Embed CreateAchievementEmbed(BaseAchievement baseAchievement, IUser user)
		{
			return Utils.Embed.WithTitle("Achievement Unlocked")
				.WithDescription($"{baseAchievement.Icon} **{user.Username}#{user.Discriminator}** has unlocked the achievement **{baseAchievement.Name}**! {baseAchievement.Icon}").Build();
		}
	}
}