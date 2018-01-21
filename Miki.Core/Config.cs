using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki
{
	public class Config
	{
		[JsonProperty("token")]
		public string Token { get; set; } = "";

		[JsonProperty("developers")]
		public List<ulong> DeveloperIds { get; set; } = new List<ulong>();

		[JsonProperty("shard_count")]
		public int ShardCount { get; set; } = 1;

		[JsonProperty("carbon_api_key")]
		public string CarbonKey { get; set; } = "";

		[JsonProperty("discord_pw_api_key")]
		public string DiscordPwKey { get; set; } = "";

		[JsonProperty("discord_bots_api_key")]
		public string DiscordBotsOrgKey { get; set; } = "";

		[JsonProperty("urban_api_key")]
		public string UrbanKey { get; set; } = "";

		[JsonProperty("imgur_api_key")]
		public string ImgurKey { get; set; } = "";

		[JsonProperty("imgur_client_id")]
		public string ImgurClientId { get; set; } = "";

		[JsonProperty("rocket_league_key")]
		public string RocketLeagueKey { get; set; } = "";

		[JsonProperty("steam_api_key")]
		public string SteamAPIKey { get; set; } = "";

		[JsonProperty("sentry_io_key")]
		public string SharpRavenKey { get; set; } = "";

		[JsonProperty("datadog_key")]
		public string DatadogKey { get; set; } = "";

		[JsonProperty("datadog_host")]
		public string DatadogHost { get; set; } = "127.0.0.1";

		[JsonProperty("connection_string")]
		public string ConnString { get; set; }
	}
}
