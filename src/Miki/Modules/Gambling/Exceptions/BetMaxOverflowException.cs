namespace Miki.Modules.Gambling.Exceptions
{
    using Miki.Localization.Exceptions;
    using Miki.Localization;

    public class BetLimitOverflowException : LocalizedException
    {
        public override IResource LocaleResource
            => new LanguageResource("error_bet_limit_overflow");
    }
}
