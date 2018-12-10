using Miki.Localization;
using Miki.Localization.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Bot.Exceptions
{
	public class BlackjackSessionNullException : LocalizedException
	{
		public override IResource LocaleResource 
			=> new LanguageResource("error_blackjack_null");
	}
}
