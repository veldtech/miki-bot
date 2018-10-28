using Miki.Localization.Exceptions;
using Miki.Models;

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