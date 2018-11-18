using Miki.Localization;
using Miki.Localization.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
	public class PastaInviteException : LocalizedException
	{
		public override IResource LocaleResource => new LanguageResource("error_pasta_invite");
	}
}
