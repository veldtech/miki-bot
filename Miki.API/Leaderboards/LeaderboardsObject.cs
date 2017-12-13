using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.Leaderboards
{
	public class LeaderboardsObject
	{
		int pageCount = 0;
		int currentPage = 0;
		int maxPages = 0;
		List<LeaderboardsItem> items = new List<LeaderboardsItem>();
	}
}
