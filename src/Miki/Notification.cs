using Microsoft.Extensions.DependencyInjection;
using Miki.Bot.Models;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using System.Threading.Tasks;
using Miki.Services.Achievements;

namespace Miki
{
	internal class Notification
	{
		public static async ValueTask SendAchievementAsync(AchievementObject d, int rank, IDiscordTextChannel channel, IDiscordUser user)
		    => await SendAchievementAsync(d.Entries[rank], channel, user);

        public static async Task SendAchievementAsync(
            AchievementEntry d, 
            IDiscordTextChannel channel, 
            IDiscordUser user)
        {
            if(channel is IDiscordGuildChannel)
            {
                using (var scope = MikiApp.Instance.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetService<MikiDbContext>();
                    var achievementSetting = await Setting.GetAsync(
                        context, 
                        channel.Id, 
                        DatabaseSettingId.Achievements);
                    if (achievementSetting != 0)
                    {
                        return;
                    }
                }
            }
            await CreateAchievementEmbed(d, user)
                .QueueAsync(channel);
        }

		private static DiscordEmbed CreateAchievementEmbed(AchievementEntry baseAchievement, IDiscordUser user)
		{
			return new EmbedBuilder()
				.SetTitle("🎉 Achievement Unlocked")
				.SetDescription($"{baseAchievement.Icon} **{user.Username}#{user.Discriminator}** has unlocked the achievement **{baseAchievement.ResourceName}**! {baseAchievement.Icon}").ToEmbed();
		}
	}
}