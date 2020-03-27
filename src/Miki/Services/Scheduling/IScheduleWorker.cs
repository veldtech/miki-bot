namespace Miki.Services.Scheduling
{
    using System;
    using System.Threading.Tasks;

    public interface IScheduleWorker
    {
        Task QueueTaskAsync(TimeSpan duration, string json);
    }
}