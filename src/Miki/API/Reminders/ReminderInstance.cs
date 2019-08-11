using Miki.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Miki.API.Reminder
{
	public class TaskInstance<T>
	{
		private TaskContainer<T> parent;
		private CancellationTokenSource cancellationToken;

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
			Task.Run(() => StartAsync());
		}

		public async Task StartAsync()
		{
			try
			{
				do
				{
					await RunTask();
				} while(RepeatReminder && !cancellationToken.IsCancellationRequested);
				parent.RemoveReminder(Id);
			}
			catch(Exception e)
			{
				Log.Error(e);
			}
		}

		public async Task<bool> RunTask()
		{
			StartTime = DateTime.Now;

			await Task.Delay((int)Length.TotalMilliseconds);

			cancellationToken.Token.ThrowIfCancellationRequested();

			Function(Context);

			return true;
		}

		public void Cancel()
		{
			cancellationToken.Cancel();
			parent.RemoveReminder(Id);
		}
	}
}