using Miki.Framework;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Commands.Nodes;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Modules.Internal.Services;

namespace Miki.Modules
{
	[Module("Internal")]
	public class InternalModule
	{
		[Service("datadog")]
		public DatadogService DatadogService { get; set; }
	}
}