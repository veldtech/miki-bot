using Miki.Configuration;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Logging;
using StatsdClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.Internal.Services
{
    public class DatadogService : BaseService
    {
		[Configurable]
		public string DatadogHost { get; set; } = "";

		public override void Install(Module m, Bot b)
		{
			var dogstatsdConfig = new StatsdConfig
			{
				StatsdServerName = Global.Config.DatadogHost,
				StatsdPort = 8125,
				Prefix = "miki"
			};

			DogStatsd.Configure(dogstatsdConfig);

			base.Install(m, b);

			var eventSystem = b.GetAttachedObject<EventSystem>();

			// TODO (Veld): reimplement this
			//b.HttpClient.RestClient.OnRequestComplete += (method, uri) =>
			//{
			//	DogStatsd.Histogram("discord.http.requests", 1, 1, new[]
			//	{
			//		$"http_method:{method}", $"http_uri:{uri}"
			//	});
			//};

			if(eventSystem != null)
			{
				var defaultHandler = eventSystem.GetCommandHandler<SimpleCommandHandler>();

				if (defaultHandler != null)
				{
					defaultHandler.OnMessageProcessed += (command, message, time) =>
					{
						if (command.Module == null)
						{
							return Task.CompletedTask;
						}

						DogStatsd.Histogram("commands.time", time, 0.1, new[] {
						$"commandtype:{command.Module.Name.ToLowerInvariant()}",
						$"commandname:{command.Name.ToLowerInvariant()}"
						});

						DogStatsd.Counter("commands.count", 1, 1, new[] {
						$"commandtype:{command.Module.Name.ToLowerInvariant()}",
						$"commandname:{command.Name.ToLowerInvariant()}"
						});

						return Task.CompletedTask;
					};
				}
			}

			Log.Message("Datadog set up!");
		}
		public override void Uninstall(Module m, Bot b)
		{
			base.Uninstall(m, b);
		}
	}
}
