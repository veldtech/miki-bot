namespace Miki.Modules
{
    using Miki.Framework.Commands.Attributes;
    using Miki.Framework.Routines;
    using Miki.Modules.Internal.Routines;

    [Module("Internal")]
	public class InternalModule
	{
        [Routine]
		public DatadogRoutine DatadogService { get; set; }
	}
}