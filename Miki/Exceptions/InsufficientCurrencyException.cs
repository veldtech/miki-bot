using Miki.Framework.Exceptions;
using Miki.Localization;
using Miki.Localization.Exceptions;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
	class InsufficientCurrencyException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_insufficient_currency", _mekos);

		private readonly long _mekos = 0;

		public InsufficientCurrencyException(object currencyOwned, int mekosRequired) : base()
		{
			_mekos = mekosRequired - (long)currencyOwned;
		}
	}
}
