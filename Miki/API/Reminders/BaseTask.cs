using Miki.Core.API.Reminder;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.Reminders
{
    public class BaseJob
    {
		public string name;

		public ulong sessionId;

		public bool repeated;

		public TimeSpan span;

		public DateTime time = new DateTime(0, 0 ,0 ,0 ,0 ,0 ,0);

		public Func<Task> function;

		internal int id;

		internal JobContainer parent;

		internal DateTime startedAt;

		internal BaseJob()
		{ }
    }
}
