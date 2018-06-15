using Miki.Configuration;
using Miki.Framework;
using Miki.Framework.Events;
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
		public string DatadogKey { get; set; } = "";

		public override void Install(Module m, Bot b)
		{
			if (string.IsNullOrWhiteSpace(DatadogKey))
			{
				Log.Warning("Datadog not initialized due to missing config");
				return;
			}

			var dogstatsdConfig = new StatsdConfig
			{
				StatsdServerName = Global.Config.DatadogHost,
				StatsdPort = 8125,
				Prefix = "miki"
			};

			DogStatsd.Configure(dogstatsdConfig);

			base.Install(m, b);

			var eventSystem = b.GetAttachedObject<EventSystem>();

			if(eventSystem != null)
			{
				eventSystem.OnCommandDone += (exception, command, message, time) =>
				{
					if (exception != null)
					{
						DogStatsd.Counter("commands.error.rate", 1);
					}

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

			Log.Message("Datadog set up!");
		}
		public override void Uninstall(Module m, Bot b)
		{
			base.Uninstall(m, b);
			
		}
	}
}
