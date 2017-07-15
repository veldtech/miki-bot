using IA;
using Miki.Accounts.Achievements.Objects;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements
{
    public class AchievementDataContainer<T> where T : BaseAchievement
    {
        public string Name = Constants.NotDefined;

        public List<T> Achievements = new List<T>();

        private AchievementDataContainer()
        {

        }
        public AchievementDataContainer(Action<AchievementDataContainer<T>> instance)
        {
            instance.Invoke(this);

            AchievementDataContainer<BaseAchievement> castedContainer = this.ToBase();
            AchievementManager.Instance.AddContainer(castedContainer);

            foreach (T d in Achievements)
            {
                d.ParentName = Name;
            }
        }
      
        public async Task CheckAsync(BasePacket packet)
        {
            using (var context = new MikiContext())
            {
                Achievement a = await context.Achievements.FindAsync(packet.discordUser.Id.ToDbLong(), Name);

                if (a == null)
                {
                    if (await Achievements[0].CheckAsync(context, packet))
                    {
                        await Achievements[0].UnlockAsync(context, packet.discordChannel, packet.discordUser);
                        await AchievementManager.Instance.CallAchievementUnlockEventAsync(Achievements[0], packet.discordUser);
                    }
                    return;
                }

                if (a.Rank >= Achievements.Count - 1)
                {
                    return;
                }

                if (await Achievements[a.Rank + 1].CheckAsync(context, packet))
                {
                    await Achievements[a.Rank + 1].UnlockAsync(context, packet.discordChannel, packet.discordUser, a.Rank + 1);
                    await AchievementManager.Instance.CallAchievementUnlockEventAsync(Achievements[a.Rank + 1], packet.discordUser);
                }
            }
        }

        public AchievementDataContainer<BaseAchievement> ToBase()
        {
            AchievementDataContainer<BaseAchievement> b = new AchievementDataContainer<BaseAchievement>();

            b.Name = Name;

            foreach(BaseAchievement a in Achievements)
            {
                b.Achievements.Add(a);
            }

            return b;
        }
    }
}