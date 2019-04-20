using Miki.Logging;
using Newtonsoft.Json;
using System;

namespace Miki
{
	public class Config
	{
		public const string MikiSettingsFile = "/miki/settings.json";

		/// <summary>
		/// This is the new Miki.Configuration file. Over time it will take over everything completely.
		/// </summary>
		public const string MikiConfigurationFile = "/miki/configuration.json";

		/// <summary>
		/// Discord API Token
		/// </summary>
		[JsonProperty("token")]
		public string Token { get; set; } = "";

		/// <summary>
		/// All user ids with admin access
		/// </summary>
		[JsonProperty("developers")]
		public ulong[] DeveloperIds { get; set; }

		/// <summary>
		/// Amount of shards for the bot to start
		/// </summary>
		[JsonProperty("shard_count")]
		public int ShardCount { get; set; } = 1;

		/// <summary>
		/// Start shard count
		/// </summary>
		[JsonProperty("shard_id")]
		public int ShardId { get; set; } = 0;

		/// <summary>
		/// Sentry Error Tracking
		/// </summary>
		[JsonProperty("sentry_io_key")]
		public string SharpRavenKey { get; set; } = "";

		/// <summary>
		/// Datadog Agent host
		/// </summary>
		[JsonProperty("datadog_host")]
		public string DatadogHost { get; set; } = "127.0.0.1";

		/// <summary>
		/// Database connection string
		/// </summary>
		[JsonProperty("connection_string")]
		public string ConnString { get; set; } = "Server=127.0.0.1;Database=miki;";

		/// <summary>
		/// Cache connection string
		/// </summary>
		[JsonProperty("redis_connection_string")]
		public string RedisConnectionString { get; set; } = "localhost";

		/// <summary>
		/// Miki API route
		/// </summary>
		[JsonProperty("miki_api_base_url")]
		public string MikiApiBaseUrl { get; set; } = "https://api.miki.ai/";

		/// <summary>
		/// Miki API Key
		/// </summary>
		[JsonProperty("miki_api_key")]
		public string MikiApiKey { get; set; } = "";

		/// <summary>
		/// Image API route
		/// </summary>
		[JsonProperty("image_api_url")]
		public string ImageApiUrl { get; internal set; } = "";

		[JsonProperty("message_worker_count")]
		public int MessageWorkerCount { get; internal set; } = 4;

		[JsonProperty("cdn_region_endpoint")]
		public string CdnRegionEndpoint { get; internal set; } = "";

		[JsonProperty("cdn_access_key")]
		public string CdnAccessKey { get; internal set; } = "";

		[JsonProperty("cdn_secret_key")]
		public string CdnSecretKey { get; internal set; } = "";

		[JsonProperty("amount_shards")]
		public int AmountShards { get; internal set; } = 1;

		[JsonProperty("rabbit_url")]
		public string RabbitUrl { get; internal set; }

		[JsonProperty("danbooru_credentials")]
		public string DanbooruCredentials { get; internal set; } = "";

		[JsonProperty("redis_endpoints")]
		public string[] RedisEndPoints { get; internal set; }

		[JsonProperty("redis_password")]
		public string RedisPassword { get; internal set; }

		[JsonProperty("self_hosted")]
		public bool SelfHosted { get; internal set; }

		[JsonProperty("loglevel")]
		public LogLevel LogLevel { get; internal set; } = LogLevel.Information;

        [JsonProperty("bunny_cdn_key")]
        public string BunnyCdnKey { get; internal set; }
    }
}
