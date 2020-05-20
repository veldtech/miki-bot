using Miki.Localization.Exceptions;
using Miki.Localization;

namespace Miki.Modules.Roles
{
    public class RoleNotSetupException : LocalizedException
    {
        /// <inheritdoc />
        public override IResource LocaleResource 
            => new LanguageResource("error_role_not_setup", ">configrole");
    }
}