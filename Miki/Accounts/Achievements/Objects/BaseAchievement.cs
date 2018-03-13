using Miki.Framework;
using Miki.Accounts.Achievements.Objects;
using Miki.Models;
using System;
using System.Threading.Tasks;
using Miki.Common;
using Discord;

namespace Miki.Accounts.Achievements
{
    public class BaseAchievement
    {
        public string Name { get; set; } = Constants.NotDefined;
        public string ParentName { get; set; } = Constants.NotDefined;

        public string Icon { get; set; } = Constants.NotDefined;
		public int Points { get; set; } = 5;

        public BaseAchievement()
        {
        }
        public BaseAchievement(Action<BaseAchievement> act)
        {
            act.Invoke(this);
        }

        public virtual async Task<bool> CheckAsync(BasePacket packet)
        {
            return true;
        }

        /// <summary>
        /// Unlocks the achievement and if not yet added to the database, It'll add it to the database.
        /// </summary>
        /// <param name="context">sql context</param>
        /// <param name="id">user id</param>
        /// <param name="r">rank set to (optional)</param>
        /// <returns></returns>
        internal async Task UnlockAsync(IMessageChannel channel, IUser user, int r = 0)
        {
            long userid = user.Id.ToDbLong();
       
			if (await UnlockIsValid(userid, r))
			{
				Notification.SendAchievement(this, channel, user);
			}
		}
		internal async Task UnlockAsync(IUser user, int r = 0)
		{
			long userid = user.Id.ToDbLong();

			if (await UnlockIsValid(userid, r))
			{
				await Notification.SendAchievementAsync(this, user);
			}
		}

		internal async Task<bool> UnlockIsValid(long userId, int newRank)
		{
			using (var context = new MikiContext())
			{
				var achievement = await Achievement.GetAsync(context, userId, Name);

				Log.Message($"achievement: {achievement}");
				Log.Message($"newrank    : {newRank}");

				// If no achievement has been found and want to unlock first
				if (achievement == null && newRank == 0)
				{
					achievement = context.Achievements.Add(new Achievement()
					{
						Id = userId,
						Name = ParentName,
						Rank = 0
					}).Entity;
				}
				// If achievement we want to unlock is the next achievement
				if (achievement != null)
				{
					if (achievement.Rank == newRank - 1)
					{
						achievement.Rank++;
					}
					else
					{
						return false;
					}

					await Achievement.UpdateCacheAsync(userId, Name, achievement);
					await context.SaveChangesAsync();
					return true;
				}
			}
			return false;
		}
	}
}