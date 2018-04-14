using Miki.Framework.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
    class RoleNullException : BotException
    {
		public override string Resource => "error_role_null";
	}
}
