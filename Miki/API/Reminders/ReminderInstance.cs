using Miki.Common;
using Miki.Common.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Miki.Core.API.Reminder
{
    public class TaskInstance<T>
    {
		TaskContainer<T> parent;
		CancellationTokenSource cancellationToken;

		public readonly T Context;

		public ulong SessionId => parent.Id;
		public int Id;
		public Action<T> Function;

		public DateTime StartTime = DateTime.Now;
		public TimeSpan Length;

		public TimeSpan TimeLeft => FinishedAt - DateTime.Now;
		public DateTime FinishedAt => StartTime + Length;

		public bool RepeatReminder { get; set; }

		public TaskInstance(int id, TaskContainer<T> parent, Action<T> fn, T context)
		{
			Id = id;
			this.parent = parent;
			Function = fn;
			cancellationToken = new CancellationTokenSource();
			Context = context;
		}

		public void Start()
		{
			Task.Run(() => RunTask(), cancellationToken.Token);
		}

		public async Task RunTask()
		{
			await Task.Delay((int)Length.TotalMilliseconds);

			cancellationToken.Token.ThrowIfCancellationRequested();

			Function(Context);

			if (RepeatReminder)
			{
				parent.CreateNewReminder(Function, Context, Length, RepeatReminder);
			}
			parent.RemoveReminder(Id);
		}

		public void Cancel()
		{
			cancellationToken.Cancel();
			parent.RemoveReminder(Id);
		}
	}
}
