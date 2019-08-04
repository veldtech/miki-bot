using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Modules.Donator.Exceptions
{
	public class InvalidKeyFormatException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_key_format_invalid");
	}
}