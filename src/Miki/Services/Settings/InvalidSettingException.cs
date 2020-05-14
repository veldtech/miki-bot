using Miki.Localization.Exceptions;
using Miki.Localization;
using System;

namespace Miki.Services.Settings
{
    public class InvalidSettingException : LocalizedException
    {
        private readonly Type enumType;

        /// <inheritdoc />
        public override IResource LocaleResource
            => new LanguageResource("error_notifications_setting_not_found", GetEnumOptionsString());

        public InvalidSettingException(Type enumType)
        {
            if(!enumType.IsAssignableFrom(typeof(Enum)))
            {
                throw new InvalidCastException($"{nameof(enumType)} requires to be an Enum type");
            }

            this.enumType = enumType;
        }

        protected string GetEnumOptionsString()
        {
            return string.Join(",", Enum.GetNames(enumType));
        }
    }
}
