using System.Collections.Generic;

namespace Miki.Modules.Overwatch.Objects
{
	public class OverwatchUserContext
	{
        public OverwatchAchievements Achievements { get; } = new OverwatchAchievements();
        public Dictionary<string, float> PlayTime { get; } = new Dictionary<string, float>();
        public OverwatchGamemode Stats { get; } = new OverwatchGamemode();

		public bool isCompetitive = false;
		public bool isValid = true;

		public OverwatchUserContext(bool competitive, OverwatchRegion response)
		{
			Achievements = response.achievements;

			if (competitive)
			{
				isCompetitive = competitive;

				PlayTime = response.heroes.playtime.competitive;
				Stats = response.stats.competitive;
			}
			else
			{
				PlayTime = response.heroes.playtime.quickplay;
				Stats = response.stats.quickplay;
			}

			if (Stats == null)
			{
				isValid = false;
				return;
			}
		}
	}
}