using Miki.Discord.Common;
using Miki.Localization;

namespace Miki.Modules.Roles
{
    public class RequiredRoleMissingException : RoleException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_role_required", role.Name);

        public RequiredRoleMissingException(IDiscordRole role)
            : base(role)
        {
        }
    }
}
