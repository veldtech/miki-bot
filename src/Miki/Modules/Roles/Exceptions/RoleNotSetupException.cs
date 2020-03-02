namespace Miki.Modules.Roles
{
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    public class RoleNotSetupException : LocalizedException
    {
        /// <inheritdoc />
        public override IResource LocaleResource 
            => new LanguageResource("error_role_not_setup", ">configrole");
    }
}