using Miki.Framework.Exceptions;
using Miki.Localization.Exceptions;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
    public abstract class PastaException : LocalizedException
    {
		protected readonly GlobalPasta _pasta;

		public PastaException(GlobalPasta pasta) : base()
		{
			_pasta = pasta;
		}
	}
}
