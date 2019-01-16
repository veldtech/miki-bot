	using Miki.Accounts.Achievements;
using Miki.Discord;
using Miki.Discord.Common;
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
            if(channel is IDiscordGuildChannel c)
            {
                using (var context = new MikiContext())
                {
                    var guild = await c.GetGuildAsync();
                    int achievementSetting = await Setting.GetAsync(context, (long)guild.Id, DatabaseSettingId.Achievements);
                    if (achievementSetting != 0)
                    {
                        return;
                    }
                }
            }

            await CreateAchievementEmbed(d, user)
                .QueueToChannelAsync(channel);
        }

		public static async Task SendAchievementAsync(IAchievement baseAchievement, IDiscordUser user)
		    => await SendAchievementAsync(baseAchievement, await user.GetDMChannelAsync(), user);

		private static DiscordEmbed CreateAchievementEmbed(IAchievement baseAchievement, IDiscordUser user)
		{
			return new EmbedBuilder()
				.SetTitle("Achievement Unlocked")
				.SetDescription($"{baseAchievement.Icon} **{user.Username}#{user.Discriminator}** has unlocked the achievement **{baseAchievement.Name}**! {baseAchievement.Icon}").ToEmbed();
		}
	}
}