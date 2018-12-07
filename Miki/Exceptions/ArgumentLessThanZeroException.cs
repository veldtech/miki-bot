using Miki.Localization;
using Miki.Localization.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
	public class ArgumentLessThanZeroException : LocalizedException
	{
		public override IResource LocaleResource => new LanguageResource("error_argument_less_than_zero");
	}
}
