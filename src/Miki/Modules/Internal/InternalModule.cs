using Miki.Attributes;
using Miki.Framework.Commands;
using Miki.Modules.Internal.Routines;

namespace Miki.Modules.Internal
{
    [Module("Internal"), Emoji(AppProps.Emoji.Developer)]
	public class InternalModule
	{
		public DatadogRoutine DatadogService { get; set; }
    }
}