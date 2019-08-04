using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Services.Blackjack.Exceptions
{
    public class BlackjackSessionExistsException : LocalizedException
    {
        public override IResource LocaleResource => new LanguageResource("error_blackjack_session_exists");
    }
}
