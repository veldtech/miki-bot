using System.Collections.Generic;

namespace Miki.Accounts
{
    public class RankedAchievementInfo : AchievementData
    {
        public int rank;
        public List<AchievementData> achievements;
    }
}