using Miki.Discord.Common;
using Miki.Localization;
using Miki.Localization.Exceptions;
using Miki.Localization.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Modules.Roles.Exceptions
{
    public class RoleException : LocalizedException
    {
        public override IResource LocaleResource => new LanguageResource("error_default");

        protected IDiscordRole _role;

        public RoleException(IDiscordRole role)
        {
            _role = role;
        }
    }
}
