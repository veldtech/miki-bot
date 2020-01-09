namespace Miki.Services.Pasta.Exceptions
{
	using Bot.Models;
    using Localization.Exceptions;

    public abstract class PastaException : LocalizedException
	{
		protected GlobalPasta Pasta;

		protected PastaException(GlobalPasta pasta) : base()
		{
			this.Pasta = pasta;
		}
	}
}