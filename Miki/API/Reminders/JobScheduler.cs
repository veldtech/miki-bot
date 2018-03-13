using Miki.API.Reminders;
using Miki.Common;
using Miki.Common.Builders;
using Miki.Core.API.Reminder;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.API
{
	public class JobScheduler
	{
		Dictionary<ulong, JobContainer> scheduledJobs = new Dictionary<ulong, JobContainer>();

		public IJob Add<T>(Action<BaseJob> taskFn) where T : BaseJob, IJob, new()
		{
			T task = new T();

			taskFn.Invoke(task);

			if (scheduledJobs.TryGetValue(task.SessionId, out JobContainer container))
			{
				return container.CreateJob(task);
			}
			else
			{
				JobContainer rc = new JobContainer();
				rc.Id = task.SessionId;
				scheduledJobs.Add(task.SessionId, rc);
				return rc.CreateJob(task);
			}
		}

		public IJob CancelJob(ulong sessionId, int reminderId)
		{
			if (scheduledJobs.TryGetValue(sessionId, out JobContainer container))
			{
				var instance = container.GetJob(reminderId);
				if (instance != null)
				{
					instance.Cancel();
					return instance;
				}
				return null;
			}
			return null;
		}

		public IJob GetInstance(ulong sessionId, int id)
		{
			if (scheduledJobs.TryGetValue(sessionId, out JobContainer container))
			{
				return container.GetJob(id);
			}
			return null;
		}

		public List<IJob> GetAllInstances(ulong sessionId)
		{
			if (scheduledJobs.TryGetValue(sessionId, out JobContainer container))
			{
				return container.GetAllJobs();
			}
			return null;
		}
	}
}