using Newtonsoft.Json;
using System.Collections.Generic;

namespace Miki.API.Leaderboards
{
	internal class LeaderboardsPage
	{
		[JsonProperty("currentPage")]
		private int CurrentPage { get; set; }

		[JsonProperty("totalPages")]
		private int PageCount { get; set; }

		[JsonProperty("items")]
		private List<LeaderboardsItem> Data { get; set; } = new List<LeaderboardsItem>();
	}
}