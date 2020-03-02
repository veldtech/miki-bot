namespace Miki
{
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    internal class RoleNullException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_role_null");
	}
}