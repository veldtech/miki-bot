using Miki.Framework.Exceptions;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
    public class PastaException : BotException
    {
		public override string Resource => "";
		public override object[] Parameters => new object[] { };

		protected GlobalPasta pasta;

		public PastaException(GlobalPasta pasta) : base()
		{
			this.pasta = pasta;
		}
	}
}
