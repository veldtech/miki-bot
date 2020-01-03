namespace Miki.API.Leaderboards
{
	using Newtonsoft.Json;
	using System.Collections.Generic;

	internal class LeaderboardsPage
	{
		[JsonProperty("currentPage")]
		public int CurrentPage { get; set; }

		[JsonProperty("totalPages")]
		public int PageCount { get; set; }

		[JsonProperty("items")]
		public List<LeaderboardsItem> Data { get; set; } = new List<LeaderboardsItem>();
	}
}