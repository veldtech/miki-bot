using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Miki.API.Reminders
{
	class TimeSpecificJob : BaseJob, IJob
	{
		CancellationTokenSource cancellationToken = new CancellationTokenSource();

		public int JobId => id;

		public string Name => name;

		public bool Repeating => repeated;

		public ulong SessionId => sessionId;

		public DateTime StartedAt => startedAt;

		public TimeSpan TimeLeft => time - DateTime.Now;

		public DateTime TicksAt => time;

		public void Cancel()
		{
			cancellationToken.Cancel();
			parent.RemoveJob(JobId);
		}

		public void Start()
		{
			Task.Run(async () => RunAsync());
		}

		private async Task RunAsync()
		{

		}
	}
}
