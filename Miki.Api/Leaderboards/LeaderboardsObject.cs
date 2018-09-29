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
		[JsonProperty("totalPages")]
		public int totalPages;

		[JsonProperty("currentPage")]
		public int currentPage;

		[JsonProperty("items")]
		public List<LeaderboardsItem> items = new List<LeaderboardsItem>();
	}
}
