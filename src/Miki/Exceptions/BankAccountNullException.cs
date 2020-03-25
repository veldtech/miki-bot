namespace Miki
{
    using Miki.Localization;
    using Miki.Localization.Exceptions;

    internal class BankAccountNullException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_bank_account_null");
	}
}