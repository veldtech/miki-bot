using System;
using System.Collections.Generic;
using System.Linq;

namespace Miki.Core.API.Reminder
{
	public class TaskContainer<T>
	{
		public ulong Id { get; set; }

		private Dictionary<int, TaskInstance<T>> instances = new Dictionary<int, TaskInstance<T>>();

		private Random random = new Random();

		/// <summary>
		/// Creates a new reminder
		/// </summary>
		/// <param name="fn">function to run at end time</param>
		/// <param name="context">context to pass</param>
		/// <param name="at">time before the function runs</param>
		/// <param name="repeated">repeat after function is done?</param>
		/// <returns></returns>
		public int CreateNewReminder(Action<T> fn, T context, TimeSpan at, bool repeated)
		{
			int? id = GetRandomKey();
			if (id == null)
			{
				return -1;
			}

			TaskInstance<T> reminder = new TaskInstance<T>(id.GetValueOrDefault(), this, fn, context);

			if (repeated)
			{
				reminder.RepeatReminder = true;
			}

			reminder.StartTime = DateTime.Now;
			reminder.Length = at;

			reminder.Start();

			instances.Add(id ?? 0, reminder);

			return id.GetValueOrDefault();
		}

		public List<TaskInstance<T>> GetAllReminders()
			=> instances.Values.ToList();

		public TaskInstance<T> GetReminder(int id)
		{
			if (instances.TryGetValue(id, out TaskInstance<T> value))
			{
				return value;
			}
			return null;
		}

		public void RemoveReminder(int id)
		{
			instances.Remove(id);
		}

		private int? GetRandomKey()
		{
			List<int> freeIds = new List<int>();
			for (int i = 1; i < 1000; i++)
			{
				freeIds.Add(i);
			}
			freeIds.RemoveAll(x => instances.ContainsKey(x));
			if (freeIds.Count > 0)
			{
				return freeIds[random.Next(0, freeIds.Count)];
			}
			return null;
		}
	}
}