using Miki.Localization;
using Miki.Localization.Exceptions;
using Miki.Localization.Models;

namespace Miki.Exceptions
{
	internal class RoleNullException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_role_null");
	}
}