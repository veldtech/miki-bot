namespace Miki
{
    using Miki.Localization;
    using Miki.Localization.Exceptions;

    internal class TimerNullException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_timer_null");
	}
}