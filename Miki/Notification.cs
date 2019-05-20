using Microsoft.Extensions.DependencyInjection;
using Miki.Accounts.Achievements;
using Miki.Bot.Models;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Models;
using System.Threading.Tasks;

namespace Miki
{
	internal class Notification
	{
		public static async ValueTask SendAchievementAsync(AchievementDataContainer d, int rank, IDiscordTextChannel channel, IDiscordUser user)
		    => await SendAchievementAsync(d.Achievements[rank], channel, user);

        public static async Task SendAchievementAsync(IAchievement d, IDiscordTextChannel channel, IDiscordUser user)
        {
            if(channel is IDiscordGuildChannel)
            {
                using (var scope = MikiApp.Instance.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetService<MikiDbContext>();
                    var achievementSetting = await Setting.GetAsync(context, channel.Id, DatabaseSettingId.Achievements);
                    if (achievementSetting != 0)
                    {
                        return;
                    }
                }
            }
            await CreateAchievementEmbed(d, user)
                .QueueAsync(channel);
        }

		public static async Task SendAchievementAsync(IAchievement baseAchievement, IDiscordUser user)
		    => await SendAchievementAsync(baseAchievement, await user.GetDMChannelAsync(), user);

		private static DiscordEmbed CreateAchievementEmbed(IAchievement baseAchievement, IDiscordUser user)
		{
			return new EmbedBuilder()
				.SetTitle("Achievement gekregen!")
				.SetDescription($"{baseAchievement.Icon} **{user.Username}#{user.Discriminator}** heeft de achievement **{baseAchievement.Name}** gekregen! {baseAchievement.Icon}").ToEmbed();
		}
	}
}