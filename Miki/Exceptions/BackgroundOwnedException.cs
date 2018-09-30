using Miki.Framework.Exceptions;
using Miki.Localization;
using Miki.Localization.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
	public class BackgroundOwnedException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_background_owned");
	}
}