using Miki.Localization;
using Miki.Localization.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Modules.Gambling.Services.Roulette.Exceptions
{
    public class TableUnauthorizedException : LocalizedException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_table_unauthorized");
    }
}
