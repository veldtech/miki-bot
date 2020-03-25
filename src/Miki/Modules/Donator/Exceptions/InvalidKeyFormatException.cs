namespace Miki.Modules.Donator.Exceptions
{
	using Miki.Localization.Exceptions;
    using Miki.Localization;

	public class InvalidKeyFormatException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_key_format_invalid");
	}
}