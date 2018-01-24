using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Miki.API.Leaderboards
{
	class LeaderboardsPage
	{
		[JsonProperty("current_page")]
		int CurrentPage { get; set; }
		
		[JsonProperty("page_count")]
		int PageCount { get; set; }

		[JsonProperty("total_count")]
		int TotalCount { get; set; }

		[JsonProperty("data")]
		List<LeaderboardsItem> Data { get; set; } = new List<LeaderboardsItem>();
	}
}
