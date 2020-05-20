using Miki.Framework.Commands;
using Miki.Modules.Internal.Routines;

namespace Miki.Modules.Internal
{
    [Module("Internal")]
	public class InternalModule
	{
		public DatadogRoutine DatadogService { get; set; }
    }
}