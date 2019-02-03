using Miki.Accounts.Achievements.Objects;
using Miki.Bot.Models;
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

		public List<IAchievement> Achievements = new List<IAchievement>();

		public AchievementDataContainer()
		{
			AchievementManager.Instance.AddContainer(this);

			foreach (IAchievement d in Achievements)
			{
				d.ParentName = Name;
			}
		}

		public AchievementDataContainer(Action<AchievementDataContainer> instance)
		{
			instance.Invoke(this);

			AchievementManager.Instance.AddContainer(this);

			foreach (IAchievement d in Achievements)
			{
				d.ParentName = Name;
			}
		}

		public async Task CheckAsync(BasePacket packet)
		{
			await InternalCheckAsync(packet);
		}

		private async Task InternalCheckAsync(BasePacket packet)
		{
			long userId = packet.discordUser.Id.ToDbLong();

			using (var context = new MikiContext())
			{
				Achievement a = await DatabaseHelpers.GetAchievementAsync(context, userId, Name);

				if (a == null)
				{
					if (await Achievements[0].CheckAsync(packet))
					{
						await AchievementManager.Instance.UnlockAsync(Achievements[0], packet.discordChannel, packet.discordUser);
						await AchievementManager.Instance.CallAchievementUnlockEventAsync(Achievements[0], packet.discordUser, packet.discordChannel);
					}
					return;
				}

				if (a.Rank >= Achievements.Count - 1)
				{
					return;
				}

				if (await Achievements[a.Rank + 1].CheckAsync(packet))
				{
					await AchievementManager.Instance.UnlockAsync(Achievements[a.Rank + 1], packet.discordChannel, packet.discordUser, a.Rank + 1);
					await AchievementManager.Instance.CallAchievementUnlockEventAsync(Achievements[a.Rank + 1], packet.discordUser, packet.discordChannel);
				}
			}
		}

		public AchievementDataContainer ToBase()
		{
			AchievementDataContainer b = new AchievementDataContainer();

			b.Name = Name;

			foreach (IAchievement a in Achievements)
			{
				b.Achievements.Add(a);
			}

			return b;
		}
	}
}