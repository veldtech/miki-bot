using System.Collections.Generic;
using Newtonsoft.Json;

namespace Miki.Api.Leaderboards
{
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