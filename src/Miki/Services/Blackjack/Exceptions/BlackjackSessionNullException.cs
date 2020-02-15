namespace Miki.Services
{
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;
    
    public class BlackjackSessionNullException : LocalizedException
	{
		public override IResource LocaleResource 
			=> new LanguageResource("error_blackjack_null");
	}
}
