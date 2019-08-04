using Miki.Bot.Models;
using System;
using System.Threading.Tasks;

namespace Miki.Accounts
{
    public class TransactionManager
    {
        public Func<User, int, Task> OnTransaction;
    }
}
