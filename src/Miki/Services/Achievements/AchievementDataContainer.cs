namespace Miki.Services.Achievements
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Accounts.Achievements.Objects;
    using Miki.Bot.Models;
    using Miki.Discord.Common;
    using Miki.Framework;
    using Miki.Helpers;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class AchievementDataContainer
	{
		public string Name { get; set; } = Constants.NotDefined;

		public List<IAchievement> Achievements { get; } = new List<IAchievement>();

        public AchievementService Service { get; }

        internal AchievementDataContainer(AchievementService parent)
            : this(parent, null)
		{
        }
        internal AchievementDataContainer(AchievementService parent, Action<AchievementDataContainer> instance)
        {
            this.Service = parent;

            instance?.Invoke(this);

            foreach(IAchievement d in Achievements)
			{
				d.ParentName = Name;
			}
		}

		public Task CheckAsync(BasePacket packet)
		{
            if(packet == null)
            {
                return Task.FromException(new ArgumentNullException(nameof(packet)));
            }
			return InternalCheckAsync(packet);
		}

        private async Task InternalCheckAsync(BasePacket packet)
        {
            long userId = (long)packet.discordUser.Id;

            using(var scope = MikiApp.Instance.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<MikiDbContext>();
                Achievement a = await DatabaseHelpers.GetAchievementAsync(context, userId, Name)
                    .ConfigureAwait(false);

                if(a == null)
                {
                    if(!await this.Achievements[0].CheckAsync(packet))
                    {
                        return;
                    }

                    await this.UnlockAsync(
                            context,
                            this.Achievements[0],
                            packet.discordChannel,
                            packet.discordUser)
                        .ConfigureAwait(false);

                    await Service.CallAchievementUnlockEventAsync(
                            context,
                            this.Achievements[0],
                            packet.discordUser,
                            packet.discordChannel)
                        .ConfigureAwait(false);

                    return;
                }

                if(a.Rank >= Achievements.Count - 1)
                {
                    return;
                }

                if(await Achievements[a.Rank + 1].CheckAsync(packet))
                {
                    await this.UnlockAsync(
                            context,
                            this.Achievements[a.Rank + 1],
                            packet.discordChannel,
                            packet.discordUser)
                        .ConfigureAwait(false);

                    await Service.CallAchievementUnlockEventAsync(
                            context,
                            this.Achievements[a.Rank + 1],
                            packet.discordUser,
                            packet.discordChannel)
                        .ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Unlocks the achievement and if not yet added to the database, It'll add it to the database.
        /// </summary>
        /// <param name="context">sql context</param>
        /// <param name="id">user id</param>
        /// <param name="r">rank set to (optional)</param>
        /// <returns></returns>
        public async Task UnlockAsync(DbContext context, IAchievement achievement, IDiscordTextChannel channel, IDiscordUser user)
        {
            await Service.CallAchievementUnlockEventAsync(context, achievement, user, channel)
                    .ConfigureAwait(false);
            await Notification.SendAchievementAsync(achievement, channel, user)
                    .ConfigureAwait(false);
        }
    }
}