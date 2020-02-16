namespace Miki.Api.Leaderboards
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class LeaderboardsObject
	{
		[JsonProperty("totalPages")]
		public int TotalPages { get; internal set; }

		[JsonProperty("currentPage")]
		public int CurrentPage { get; internal set; }

        [JsonProperty("items")]
		public List<LeaderboardsItem> Items { get; internal set; } = new List<LeaderboardsItem>();
	}
}