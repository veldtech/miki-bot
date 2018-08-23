using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
    public class UserBannedException : UserException
    {
		public override string Resource => "error_user_banned";

		public UserBannedException(User user) : base(user)
		{

		}
	}
}
