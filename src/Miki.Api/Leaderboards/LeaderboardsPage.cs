namespace Miki.Api.Leaderboards
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

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