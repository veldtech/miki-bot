using Miki.Localization.Exceptions;
using Miki.Localization;

namespace Miki.Modules.Gambling.Exceptions
{
    public class BetLimitOverflowException : LocalizedException
    {
        public override IResource LocaleResource
            => new LanguageResource("error_bet_limit_overflow", maxBet.ToString("N0"));

        private readonly int maxBet;

        public BetLimitOverflowException(int maxBet)
        {
            this.maxBet = maxBet;
        }
    }
}
