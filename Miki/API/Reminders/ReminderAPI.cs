using Miki.Common;
using Miki.Common.Builders;
using Miki.Core.API.Reminder;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.API.Reminder
{
    public class ReminderAPI
    {
		Dictionary<ulong, ReminderContainer> reminders = new Dictionary<ulong, ReminderContainer>(); 

        public int AddReminder(IDiscordUser user, string reminder, TimeSpan atTime, bool repeated = false)
        {
			if(reminders.TryGetValue(user.Id, out ReminderContainer container))
			{
				return container.CreateNewReminder(user, reminder, atTime, repeated);
			}
			else
			{
				ReminderContainer rc = new ReminderContainer();
				rc.Id = user.Id;
				reminders.Add(user.Id, rc);
				return rc.CreateNewReminder(user, reminder, atTime, repeated);			
			}
        }

		public ReminderInstance CancelReminder(IDiscordUser user, int reminderId)
		{
			if (reminders.TryGetValue(user.Id, out ReminderContainer container))
			{
				var instance = container.GetReminder(reminderId);
				if(instance != null)
				{
					instance.Cancel();
					return instance;
				}
				return null;
			}
			return null;
		}

		public ReminderInstance GetInstance(IDiscordUser user, int id)
		{
			if (reminders.TryGetValue(user.Id, out ReminderContainer container))
			{
				return container.GetReminder(id);
			}
			return null;
		}

		public List<ReminderInstance> GetAllInstances(IDiscordUser user)
		{
			if (reminders.TryGetValue(user.Id, out ReminderContainer container))
			{
				return container.GetAllReminders();
			}
			return null;
		}
    }
}