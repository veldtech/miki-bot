using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
    class LevelAchievement : BaseAchievement
    {
        public Func<LevelPacket, Task<bool>> CheckLevel;

        public override async Task<bool> CheckAsync(MikiContext context, BasePacket packet)
        {
            return await CheckLevel(packet as LevelPacket);
        }
    }
}
