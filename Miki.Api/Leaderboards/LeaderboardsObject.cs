using Newtonsoft.Json;
using System.Collections.Generic;

namespace Miki.API.Leaderboards
{
	public class LeaderboardsObject
	{
		[JsonProperty("totalPages")]
		public int totalPages;

		[JsonProperty("currentPage")]
		public int currentPage;

		[JsonProperty("items")]
		public List<LeaderboardsItem> items = new List<LeaderboardsItem>();
	}
}