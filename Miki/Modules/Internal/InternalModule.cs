
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Modules.Internal.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Modules
{
	[Module("Internal")]
    public class InternalModule
    {
		[Service("datadog")]
		public DatadogService DatadogService { get; set; }

		public InternalModule(Module module, Framework.Bot bot)
		{

		}
    }
}
