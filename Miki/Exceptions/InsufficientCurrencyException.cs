using Miki.Framework.Exceptions;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
	class InsufficientCurrencyException : BotException
	{
		public override string Resource => "error_insufficient_currency";
		public override object[] Parameters => new object[] { mekos };

		private int mekos = 0;

		public InsufficientCurrencyException(int currencyOwned, int mekosRequired) : base()
		{
			mekos = mekosRequired - currencyOwned;
		}
	}
}
