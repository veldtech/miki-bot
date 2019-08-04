using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Services.Blackjack.Exceptions
{
    public class BlackjackSessionNullException : LocalizedException
    {
        public override IResource LocaleResource
            => new LanguageResource("error_blackjack_null");
    }
}
