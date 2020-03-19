namespace Miki
{
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    internal class BankAccountNullException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_bank_account_null");
	}
}