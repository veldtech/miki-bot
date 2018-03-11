using Discord;
using Miki.Framework;
using Miki.Common;
using Miki.Common.Interfaces;
using Miki.Accounts.Achievements;
using Miki.Models;
using System.Threading.Tasks;
using System;

namespace Miki
{
	internal class Notification
	{
		public static void SendAchievement(AchievementDataContainer d, int rank, IDiscordMessageChannel channel, IDiscordUser user)
		{
			return; //SendAchievement(d.Achievements[rank], channel, user);
		}
		public static void SendAchievement(BaseAchievement d, IDiscordMessageChannel channel, IDiscordUser user)
		{
			return; //CreateAchievementEmbed(d, user).QueueToChannel(channel);	
		}
		public static void SendAchievement(BaseAchievement baseAchievement, IDiscordUser user)
		{
			return; //CreateAchievementEmbed(baseAchievement, user).QueueToUser(user);	
		}

		private static IDiscordEmbed CreateAchievementEmbed(BaseAchievement baseAchievement, IDiscordUser user)
		{
			return Utils.Embed.SetTitle("Achievement Unlocked")
				.SetDescription($"{baseAchievement.Icon} **{user.Username}#{user.Discriminator}** has unlocked the achievement **{baseAchievement.Name}**! {baseAchievement.Icon}");
		}
	}
}