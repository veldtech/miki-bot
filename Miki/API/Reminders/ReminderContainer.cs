using Miki.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Miki.Core.API.Reminder
{
    public class ReminderContainer
    {
		public ulong Id { get; set; }
		Dictionary<int, ReminderInstance> instances = new Dictionary<int, ReminderInstance>();

		Random random = new Random();

		public int CreateNewReminder(IDiscordUser user, string text, TimeSpan at, bool repeated)
		{
			int? id = GetRandomKey();
			if(id == null)
			{
				return -1;
			}

			ReminderInstance reminder = new ReminderInstance(id.GetValueOrDefault(), this, text);

			if(repeated)
			{
				reminder.RepeatReminder = true;
			}

			reminder.StartTime = DateTime.Now;
			reminder.Length = at;

			reminder.Start(user);

			instances.Add(id ?? 0, reminder);

			return id.GetValueOrDefault();
		}
		public int CreateNewReminder(IDiscordMessageChannel channel, string text, TimeSpan at, bool repeated)
		{
			int? id = GetRandomKey();
			if (id == null)
			{
				return -1;
			}

			ReminderInstance reminder = new ReminderInstance(id.GetValueOrDefault(), this, text);

			if (repeated)
			{
				reminder.RepeatReminder = true;
			}

			reminder.StartTime = DateTime.Now;
			reminder.Length = at;

			reminder.Start(channel);

			instances.Add(id ?? 0, reminder);

			return id.GetValueOrDefault();
		}

		public List<ReminderInstance> GetAllReminders()
			=> instances.Values.ToList();

		public ReminderInstance GetReminder(int id)
		{
			if(instances.TryGetValue(id, out ReminderInstance value))
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
