namespace Miki.Modules.Roles.Exceptions
{
    using Miki.Discord.Common;
    using Miki.Localization.Models;

    public class RequiredRoleMissingException : RoleException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_role_required", _role.Name);

        public RequiredRoleMissingException(IDiscordRole role)
            : base(role)
        {
        }
    }
}
