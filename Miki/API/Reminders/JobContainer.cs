using Miki.API.Reminders;
using Miki.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Miki.Core.API.Reminder
{
    public class JobContainer
    {
		public ulong Id { get; set; }

		Dictionary<int, IJob> instances = new Dictionary<int, IJob>();

		Random random = new Random();

		public IJob CreateJob(IJob task)
		{
			BaseJob reminder = (task as BaseJob);

			int? id = GetRandomKey();
			if (id == null)
			{
				throw new ArgumentOutOfRangeException("TaskId", "Too many tasks running on one container.");
			}

			reminder.id = id.Value;
			reminder.parent = this;

			if (reminder is IJob t)
			{
				instances.Add(id.Value, t);
				return t;
			}
			throw new ArgumentException("Type is not able to be cast to ITask");
		}


		public List<IJob> GetAllJobs()
			=> instances.Values.ToList();

		public IJob GetJob(int id)
		{
			if(instances.TryGetValue(id, out IJob value))
			{
				return value;
			}
			return null;
		}

		public void RemoveJob(int id)
		{
			// TODO: think if I should cancel here or in the member function
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
