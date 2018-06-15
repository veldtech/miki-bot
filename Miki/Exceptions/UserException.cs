using Miki.Framework.Exceptions;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
    public class UserException : BotException
    {
		public override string Resource => "error_default_user";
		public readonly User User;

		public UserException(User user) : base()
		{
			User = user;
		}
	}
}
