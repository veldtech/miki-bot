using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Exceptions
{
	internal class InsufficientCurrencyException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_insufficient_currency", _mekos);

		private readonly long _mekos = 0;

		public InsufficientCurrencyException(long currencyOwned, int mekosRequired) : base()
		{
			_mekos = (long)mekosRequired - currencyOwned;
		}
	}
}