namespace Miki
{
    using Miki.Localization.Exceptions;
    using Miki.Localization;

    internal class RoleNullException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_role_null");
	}
}