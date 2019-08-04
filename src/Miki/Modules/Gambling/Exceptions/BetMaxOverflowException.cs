using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Modules.Gambling.Exceptions
{
    public class BetLimitOverflowException : LocalizedException
    {
        public override IResource LocaleResource
            => new LanguageResource("error_bet_limit_overflow");
    }
}
