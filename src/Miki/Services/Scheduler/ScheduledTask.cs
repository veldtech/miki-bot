namespace Miki.Services.Scheduler
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class ScheduledTask
    {
        public int Id;
        public DateTime TimeStarted;
        public TimeSpan TimeSpan;
    }
}
