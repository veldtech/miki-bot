using Miki.Localization;
using Miki.Localization.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Modules.Gambling.Exceptions
{
    public class BetLimitOverflowException : LocalizedException
    {
        public override IResource LocaleResource
            => new LanguageResource("error_bet_limit_overflow");
    }
}
