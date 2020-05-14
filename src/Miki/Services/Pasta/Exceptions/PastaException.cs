using Miki.Bot.Models;
using Miki.Localization.Exceptions;

namespace Miki.Services.Pasta.Exceptions
{
    public abstract class PastaException : LocalizedException
	{
		protected GlobalPasta Pasta;

		protected PastaException(GlobalPasta pasta) : base()
		{
			this.Pasta = pasta;
		}
	}
}