namespace Miki.Modules
{
    using Miki.Framework.Commands.Attributes;
    using Miki.Modules.Internal.Routines;

    [Module("Internal")]
	public class InternalModule
	{
		//[Service("datadog")]
		public DatadogRoutine DatadogService { get; set; }
	}
}