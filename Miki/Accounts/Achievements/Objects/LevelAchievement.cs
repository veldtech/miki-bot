using System;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
    internal class LevelAchievement : IAchievement
    {
        public Func<LevelPacket, Task<bool>> CheckLevel;

        public string Name { get; set; }
        public string ParentName { get; set; }
        public string Icon { get; set; }
        public int Points { get; set; }

        public async Task<bool> CheckAsync(BasePacket packet)
        {
            return await CheckLevel(packet as LevelPacket);
        }
    }
}