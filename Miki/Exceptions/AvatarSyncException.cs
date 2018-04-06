using Miki.Framework.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
	public class AvatarSyncException : BotException
	{
		public override string Resource => "error_avatar_sync";

		public AvatarSyncException() : base()
		{ }
	}
}
