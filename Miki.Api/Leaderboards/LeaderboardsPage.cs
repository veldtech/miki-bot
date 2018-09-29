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
		[JsonProperty("currentPage")]
		int CurrentPage { get; set; }
		
		[JsonProperty("totalPages")]
		int PageCount { get; set; }

		[JsonProperty("items")]
		List<LeaderboardsItem> Data { get; set; } = new List<LeaderboardsItem>();
	}
}
