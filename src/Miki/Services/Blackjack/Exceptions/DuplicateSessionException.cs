using Miki.Localization.Exceptions;
using Miki.Localization;

namespace Miki.Services
{
    public class DuplicateSessionException : LocalizedException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_blackjack_session_exists");
    }
}
