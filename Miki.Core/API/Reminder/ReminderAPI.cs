using IA.SDK.Builders;
using System;
using System.Threading.Tasks;

namespace Miki.API
{
    public class ReminderAPI
    {
        private ulong userId = 0;

        private string value = Constants.NotDefined;

        private TimeSpan timeTaken = new TimeSpan();

        public ReminderAPI(ulong receiver)
        {
            userId = receiver;
        }

        public ReminderAPI Remind(string reminder, TimeSpan delay)
        {
            timeTaken = delay;
            value = reminder;
            return this;
        }

        public async Task Listen()
        {
            await Task.Delay(timeTaken);

            await Utils.Embed
                .SetTitle("⏰ Reminder")
                .SetDescription(new MessageBuilder()
                    .AppendText(value)
                    .BuildWithBlockCode(""))
                .SendToUser(userId);
        }
    }
}