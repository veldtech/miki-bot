using Miki.Localization.Exceptions;
using Miki.Localization;

namespace Miki.Exceptions
{
	public class BackgroundOwnedException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_background_owned");
	}
}