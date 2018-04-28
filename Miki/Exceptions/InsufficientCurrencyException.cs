using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
	class InsufficientCurrencyException : UserException
	{
		public override string Resource => "error_insufficient_currency";
		public override object[] Parameters => new object[] { mekosRequired - User.Currency };

		private int mekosRequired = 0;

		public InsufficientCurrencyException(User user, int mekosRequired) : base(user)
		{
			this.mekosRequired = mekosRequired;
		}
	}
}
