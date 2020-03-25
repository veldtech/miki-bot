namespace Miki.Exceptions
{
    using Miki.Localization.Exceptions;
    using Miki.Localization;

	public class BackgroundNotOwnedException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_background_not_owned");
	}
}