using Miki.Discord.Common;
using Miki.Localization;
using Miki.Localization.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Modules.Roles.Exceptions
{
    public class RequiredRoleMissingException : RoleException
    {
        public override IResource LocaleResource => new LanguageResource("error_role_required", _role.Name);

        public RequiredRoleMissingException(IDiscordRole role)
            : base(role)
        {
        }
    }
}
