using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
    class AchievementAchievement : BaseAchievement
    {
        public Func<AchievementPacket, Task<bool>> CheckAchievement;

        public override async Task<bool> CheckAsync(MikiContext context, BasePacket packet)
        {
            return await CheckAchievement(packet as AchievementPacket);
        }
    }
}
