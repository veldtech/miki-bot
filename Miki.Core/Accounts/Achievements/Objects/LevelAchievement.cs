using Miki.Models;
using System;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
    internal class LevelAchievement : BaseAchievement
    {
        public Func<LevelPacket, Task<bool>> CheckLevel;

        public override async Task<bool> CheckAsync(BasePacket packet)
        {
            return await CheckLevel(packet as LevelPacket);
        }
    }
}