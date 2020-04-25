namespace Miki.Services.Scheduling
{
    using System;
    using System.Threading.Tasks;
    using Miki.Functional;

    public interface IScheduleWorker
    {
        /// <summary>
        /// Cancels a task if exists. Cannot delete owned tasks without the owner ID. Unowned tasks
        /// should pass only an UUID.
        /// </summary>
        Task CancelTaskAsync(string uuid, Optional<string> ownerId);

        Task<TaskPayload> GetTaskAsync(string ownerId, string uuid);

        /// <summary>
        /// Queues a task with a pre-specified uuid.
        /// </summary>
        Task<TaskPayload> QueueTaskAsync(
            TimeSpan duration, string uuid, string ownerId, string json, bool isRepeating);

    }
}