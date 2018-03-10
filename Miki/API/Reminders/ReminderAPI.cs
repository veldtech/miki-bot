using Miki.Common;
using Miki.Common.Builders;
using Miki.Core.API.Reminder;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.API
{
	public class TaskScheduler<T>
	{
		Dictionary<ulong, TaskContainer<T>> scheduledTasks = new Dictionary<ulong, TaskContainer<T>>();

		public int AddTask(ulong sessionId, Action<T> fn, T context, TimeSpan atTime, bool repeated = false)
		{
			if (scheduledTasks.TryGetValue(sessionId, out TaskContainer<T> container))
			{
				return container.CreateNewReminder(fn, context, atTime, repeated);
			}
			else
			{
				TaskContainer<T> rc = new TaskContainer<T>();
				rc.Id = sessionId;
				scheduledTasks.Add(sessionId, rc);
				return rc.CreateNewReminder(fn, context, atTime, repeated);
			}
		}

		public TaskInstance<T> CancelReminder(ulong sessionId, int reminderId)
		{
			if (scheduledTasks.TryGetValue(sessionId, out TaskContainer<T> container))
			{
				var instance = container.GetReminder(reminderId);
				if (instance != null)
				{
					instance.Cancel();
					return instance;
				}
				return null;
			}
			return null;
		}

		public TaskInstance<T> GetInstance(ulong sessionId, int id)
		{
			if (scheduledTasks.TryGetValue(sessionId, out TaskContainer<T> container))
			{
				return container.GetReminder(id);
			}
			return null;
		}

		public List<TaskInstance<T>> GetAllInstances(ulong sessionId)
		{
			if (scheduledTasks.TryGetValue(sessionId, out TaskContainer<T> container))
			{
				return container.GetAllReminders();
			}
			return null;
		}
	}
}