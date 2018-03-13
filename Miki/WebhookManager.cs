using Miki.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Newtonsoft;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki
{
	public class WebhookManager
	{
		private static StackExchangeRedisCacheClient jsonRedisClient = new StackExchangeRedisCacheClient(new NewtonsoftSerializer(), Global.Config.RedisConnectionString);

		public static event Func<WebhookResponse, Task> OnEvent;

		public static void Listen(string v)
		{
			RedisChannel webhookChannel = new RedisChannel(v, RedisChannel.PatternMode.Auto);

			jsonRedisClient.SubscribeAsync<WebhookResponse>(webhookChannel, async (value) =>
			{
				if (OnEvent.GetInvocationList().Length > 0)
				{
					try
					{
						await OnEvent(value);
					}
					catch (Exception e)
					{
						Log.Error(e);
					}
				}
			});
		}
	}

	public class WebhookResponse
	{
		[JsonProperty("auth_code")]
		public string auth_code;

		public string data;
	}
}
