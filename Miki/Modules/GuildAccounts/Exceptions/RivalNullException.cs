using Miki.Localization;
using Miki.Localization.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Modules.GuildAccounts.Exceptions
{
    public class RivalNullException : LocalizedException
    {
        public override IResource LocaleResource
            => new LanguageResource("error_guild_rival_null");
    }
}
