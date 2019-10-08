namespace Miki.Modules.Internal
{
    using Framework.Commands.Attributes;
    using Framework.Routines;
    using Routines;

    [Module("Internal")]
	public class InternalModule
	{
        [Routine]
		public DatadogRoutine DatadogService { get; set; }
	}
}