using Newtonsoft.Json;

namespace Miki.API.Leaderboards
{
    public class LeaderboardsItem
    {
		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("name")]
        public string Name { get; set; }

		[JsonProperty("score")]
        public int Value { get; set; }

		[JsonProperty("avatar")]
		public string Avatar { get; set; }
    }
}