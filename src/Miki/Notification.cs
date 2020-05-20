using Microsoft.Extensions.DependencyInjection;
using Miki.Bot.Models;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using System.Threading.Tasks;
using Miki.Modules;
using Miki.Services.Achievements;
using Miki.Services.Settings;

namespace Miki
{
    internal class Notification
	{
		public static async ValueTask SendAchievementAsync(
            AchievementObject d, int rank, IDiscordTextChannel channel, IDiscordUser user)
		    => await SendAchievementAsync(d.Entries[rank], channel, user);

        public static async Task SendAchievementAsync(
            AchievementEntry d, IDiscordTextChannel channel, IDiscordUser user)
        {
            using var scope = MikiApp.Instance.Services.CreateScope();
            if(channel is IDiscordGuildChannel)
            {
                var context = scope.ServiceProvider.GetService<ISettingsService>();
                var achievementSetting = await context.GetAsync<AchievementNotificationSetting>(
                    SettingType.Achievements, (long)channel.Id);
                if(achievementSetting != AchievementNotificationSetting.All)
                {
                    return;
                }
            }

            var worker = scope.ServiceProvider.GetService<IMessageWorker<IDiscordMessage>>();
            await CreateAchievementEmbed(d, user).QueueAsync(worker, channel);
        }

		private static DiscordEmbed CreateAchievementEmbed(
            AchievementEntry baseAchievement, IDiscordUser user)
		{
			return new EmbedBuilder()
				.SetTitle("🎉 Achievement Unlocked")
				.SetDescription($"{baseAchievement.Icon} **{user.Username}#{user.Discriminator}** has unlocked the achievement **{baseAchievement.ResourceName}**! {baseAchievement.Icon}").ToEmbed();
		}
	}
}