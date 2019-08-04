using System.Collections.Generic;

namespace Miki.Modules.Overwatch.Objects
{
	public class OverwatchUserContext
	{
		public OverwatchAchievements Achievements => _achievements;
		public Dictionary<string, float> PlayTime => _playTime;
		public OverwatchGamemode Stats => _stats;

		private OverwatchAchievements _achievements = new OverwatchAchievements();
		private Dictionary<string, float> _playTime = new Dictionary<string, float>();
		private OverwatchGamemode _stats = new OverwatchGamemode();

		public bool isCompetitive = false;
		public bool isValid = true;

		public OverwatchUserContext(bool competitive, OverwatchRegion response)
		{
			_achievements = response.achievements;

			if(competitive)
			{
				isCompetitive = competitive;

				_playTime = response.heroes.playtime.competitive;
				_stats = response.stats.competitive;
			}
			else
			{
				_playTime = response.heroes.playtime.quickplay;
				_stats = response.stats.quickplay;
			}

			if(_stats == null)
			{
				isValid = false;
				return;
			}
		}
	}
}