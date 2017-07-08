using IA.SDK.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API
{
    public class ReminderAPI
    {
        ulong userId = 0;

        string value = Constants.NotDefined;

        TimeSpan timeTaken = new TimeSpan();

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
