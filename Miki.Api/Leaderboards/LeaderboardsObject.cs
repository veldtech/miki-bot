using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.Leaderboards
{
	public class LeaderboardsObject
	{
		[JsonProperty("total_page")]
		public int totalItems;

		[JsonProperty("current_page")]
		public int currentPage;

		[JsonProperty("data")]
		public List<LeaderboardsItem> items = new List<LeaderboardsItem>();
	}
}
