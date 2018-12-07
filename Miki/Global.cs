using Amazon.S3;
using Miki.Cache;
using Miki.Cache.StackExchange;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.FileHandling;
using Miki.Models.Objects.Backgrounds;
using Miki.Serialization.Protobuf;
using Newtonsoft.Json;
using SharpRaven;
using StackExchange.Redis;
using System;

namespace Miki
{
	/// <summary>
	/// Global data for constant folder structures and versioning.
	/// </summary>
	public class Global
	{
		public static RavenClient ravenClient;

		internal static Lazy<ICacheClient> redisClientPool = new Lazy<ICacheClient>(() =>
		{
			return new StackExchangeCacheClient(new ProtobufSerializer(), ConnectionMultiplexer.Connect(Config.RedisConnectionString));
		});

		public static IExtendedCacheClient RedisClient => redisClientPool.Value as IExtendedCacheClient;

		public static DiscordApiClient ApiClient { get; set; }

		public static IDiscordUser CurrentUser { get; set; }

		public static BackgroundStore Backgrounds { get; set; }

		public static DiscordBot Client { get; set; }

		public static IGateway Gateway { get; set; }

		public static Config Config
		{
			get
			{
				if (config == null)
				{
					if (FileReader.FileExist("settings.json", "miki"))
					{
						FileReader reader = new FileReader("settings.json", "miki");
						config = JsonConvert.DeserializeObject<Config>(reader.ReadAll());
						reader.Finish();
					}
					else
					{
						FileWriter writer = new FileWriter("settings.json", "miki");
						writer.Write(JsonConvert.SerializeObject(new Config(), Formatting.Indented));
						writer.Finish();
						config = new Config();
					}
				}
				return config;
			}
		}

		public static AmazonS3Client CdnClient
		{
			get
			{
				if (cdnClient == null)
				{
					cdnClient = new AmazonS3Client(Config.CdnAccessKey, Config.CdnSecretKey, new AmazonS3Config()
					{
						ServiceURL = Config.CdnRegionEndpoint
					});
				}
				return cdnClient;
			}
		}

		private static Config config = null;
		private static AmazonS3Client cdnClient;
	}

	public class Constants
	{
		public const string NotDefined = "$not-defined";
	}
}