namespace Miki.Services.Scheduling
{
    using System;
    using System.Threading.Tasks;

    public static class ScheduleWorkerExtensions
    {
        public static Task<TaskPayload> QueueTaskAsync(
            this IScheduleWorker worker,
            TimeSpan duration,
            string ownerId,
            string json,
            bool isRepeating)
        {
            return worker.QueueTaskAsync(
                duration, Guid.NewGuid().ToString(), ownerId, json, isRepeating);
        }
    }
}
