using Miki.Discord.Common;
using Miki.Localization;

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
