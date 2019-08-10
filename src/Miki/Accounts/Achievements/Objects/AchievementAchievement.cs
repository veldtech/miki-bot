using Miki.Logging;
using System;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
	internal class AchievementAchievement : IAchievement
	{
		public Func<AchievementPacket, Task<bool>> CheckAchievement;

        public string Name { get; set; }

        public string ParentName { get; set; }
        public string Icon { get; set; }

        public int Points { get; set; }

        public async Task<bool> CheckAsync(BasePacket packet)
        {
            if (packet is AchievementPacket p)
            {
                return await CheckAchievement(p);
            }
            Log.Warning("Packet was expected to be 'AchievementPacket' was not correct.");
            return false;
        }
	}
}