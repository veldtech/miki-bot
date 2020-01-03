namespace Miki.Modules.Internal
{
    using Framework.Commands.Attributes;
    using Routines;

    [Module("Internal")]
	public class InternalModule
	{
		public DatadogRoutine DatadogService { get; set; }
	}
}