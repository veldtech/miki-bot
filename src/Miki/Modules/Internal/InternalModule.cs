namespace Miki.Modules.Internal
{
    using Miki.Framework.Commands;
    using Routines;

    [Module("Internal")]
	public class InternalModule
	{
		public DatadogRoutine DatadogService { get; set; }
	}
}