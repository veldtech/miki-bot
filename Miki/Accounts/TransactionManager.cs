using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Accounts
{
	public class TransactionManager
	{
		public Func<User, int, Task> OnTransaction;
	}
}
