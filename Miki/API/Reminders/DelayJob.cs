using Miki.API.Reminders;
using Miki.Common;
using Miki.Common.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Miki.Core.API.Reminder
{
    public class DelayTask : BaseJob, IJob
    {
		CancellationTokenSource cancellationToken = new CancellationTokenSource();

		public ulong SessionId => sessionId;
		public int JobId => id;

		public string Name => name;
		
		public DateTime StartedAt => startedAt;

		public TimeSpan TimeLeft => TicksAt - DateTime.Now;

		public DateTime TicksAt => StartedAt + span;

		public bool Repeating => repeated;

		public DelayTask()
		{ }

		public void Start()
		{
			Task.Run(() => RunTask(), cancellationToken.Token);
		}

		public virtual async Task RunTask()
		{
			await Task.Delay((int)span.TotalMilliseconds);

			if (!cancellationToken.Token.IsCancellationRequested)
			{
				await function();

				if (repeated)
				{
					parent.CreateJob(this);
				}
			}
			parent.RemoveJob(id);
		}

		public void Cancel()
		{
			cancellationToken.Cancel();
			parent.RemoveJob(id);
		}
	}
}
