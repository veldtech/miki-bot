using Miki.Localization.Exceptions;
using Miki.Localization;

namespace Miki.Exceptions
{
	public class BackgroundNotOwnedException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_background_not_owned");
	}
}