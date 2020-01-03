namespace Miki.Exceptions
{
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    public class BackgroundOwnedException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_background_owned");
	}
}