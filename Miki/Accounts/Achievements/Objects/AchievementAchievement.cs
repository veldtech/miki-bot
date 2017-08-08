using Miki.Models;
using System;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
    internal class AchievementAchievement : BaseAchievement
    {
        public Func<AchievementPacket, Task<bool>> CheckAchievement;

        public override async Task<bool> CheckAsync(MikiContext context, BasePacket packet)
        {
            return await CheckAchievement(packet as AchievementPacket);
        }
    }
}