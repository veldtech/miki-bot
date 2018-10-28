using Newtonsoft.Json;

namespace Miki.API.Leaderboards
{
	public class LeaderboardsItem
	{
		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("username")]
		public string Name { get; set; }

		[JsonProperty("score")]
		public int Value { get; set; }

		[JsonProperty("rank")]
		public int Rank { get; set; }
	}
}