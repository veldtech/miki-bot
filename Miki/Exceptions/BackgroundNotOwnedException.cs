using Miki.Framework.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
    public class BackgroundNotOwnedException : BotException
    {
		public override string Resource => "error_background_not_owned";
	}
}
