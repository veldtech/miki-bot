using Miki.Accounts.Achievements.Objects;
using Miki.Framework;
using Miki.Helpers;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements
{
	public class AchievementDataContainer
	{
		public string Name = Constants.NotDefined;

		public List<BaseAchievement> Achievements = new List<BaseAchievement>();

		public AchievementDataContainer()
		{
			foreach (BaseAchievement d in Achievements)
			{
				d.ParentName = Name;
			}
		}

		public AchievementDataContainer(Action<AchievementDataContainer> instance)
		{
			instance.Invoke(this);

			foreach (BaseAchievement d in Achievements)
			{
				d.ParentName = Name;
			}
		}

		public async Task<bool> CheckAsync(BasePacket packet)
			=> await InternalCheckAsync(packet);

		private async Task<bool> InternalCheckAsync(BasePacket packet)
		{
			long userId = packet.discordUser.Id.ToDbLong();

			using (var context = new MikiContext())
			{
				Achievement a = await DatabaseHelpers.GetAchievementAsync(context, userId, Name);

				if (a == null)
				{
					if (await Achievements[0].CheckAsync(packet))
					{
						await Achievements[0].UnlockAsync(packet.discordChannel, packet.discordUser);
						return true;
					}
					return false;
				}

				if (a.Rank >= Achievements.Count - 1)
				{
					return false;
				}

				if (await Achievements[a.Rank + 1].CheckAsync(packet))
				{
					await Achievements[a.Rank + 1].UnlockAsync(packet.discordChannel, packet.discordUser, a.Rank + 1);
					return true;
				}
			}
			return false;
		}

		public AchievementDataContainer ToBase()
		{
			AchievementDataContainer b = new AchievementDataContainer();

			b.Name = Name;

			foreach (BaseAchievement a in Achievements)
			{
				b.Achievements.Add(a);
			}

			return b;
		}
	}
}