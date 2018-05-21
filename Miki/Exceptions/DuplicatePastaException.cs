using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
    public class DuplicatePastaException : PastaException
    {
		public override string Resource => "miki_module_pasta_create_error_already_exist";
		public override object[] Parameters => new object[] { $"`{pasta.Id}`" };

		public DuplicatePastaException(GlobalPasta pasta) : base(pasta)
		{
		}
    }
}
