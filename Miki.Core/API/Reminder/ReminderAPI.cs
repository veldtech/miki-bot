using IA.SDK.Builders;
using IA.SDK.Interfaces;
using Miki.Core.API.Reminder;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.API.Reminder
{
    public class ReminderAPI
    {
		Dictionary<ulong, ReminderContainer> reminders = new Dictionary<ulong, ReminderContainer>(); 

        public int AddReminder(IDiscordUser targetUser, string reminder, TimeSpan atTime, bool repeated = false)
        {
			if(reminders.TryGetValue(targetUser.Id, out ReminderContainer container))
			{
				return container.CreateNewReminder(targetUser, reminder, atTime, repeated);
			}
			else
			{
				ReminderContainer rc = new ReminderContainer();
				rc.Id = targetUser.Id;
				reminders.Add(targetUser.Id, rc);
				return rc.CreateNewReminder(targetUser, reminder, atTime, repeated);			
			}
        }

		public ReminderInstance CancelReminder(IDiscordUser user, int id)
		{
			if (reminders.TryGetValue(user.Id, out ReminderContainer container))
			{
				var instance = container.GetReminder(id);
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