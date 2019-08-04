using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Exceptions
{
    public class BackgroundOwnedException : LocalizedException
    {
        public override IResource LocaleResource
            => new LanguageResource("error_background_owned");
    }
}