using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Exceptions
{
    internal class RoleNullException : LocalizedException
    {
        public override IResource LocaleResource
            => new LanguageResource("error_role_null");
    }
}