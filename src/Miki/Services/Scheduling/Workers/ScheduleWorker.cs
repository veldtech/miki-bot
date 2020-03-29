namespace Miki.Services.Scheduling
{
    using System;
    using System.Threading.Tasks;
    using Miki.Cache;
    using Miki.Functional;

    public class ScheduleWorker : IScheduleWorker
    {
        protected readonly string taskName;
        protected readonly IExtendedCacheClient cacheClient;
        protected readonly ISchedulerService parent;

        public ScheduleWorker(
            string taskName, ISchedulerService parent, IExtendedCacheClient cacheClient)
        {
            this.taskName = taskName;
            this.cacheClient = cacheClient;
            this.parent = parent;
        }

        /// <inheritdoc />
        public virtual async Task QueueTaskAsync(
            TimeSpan duration, string json, string ownerId, bool isRepeating)
        {
            await QueueTaskAsync(CreateWorkPayload(duration, json, ownerId, isRepeating));
        }

        /// <inheritdoc />
        public async Task CancelTaskAsync(string uuid, Optional<string> ownerId)
        {
            await cacheClient.HashDeleteAsync(parent.GetObjectNamespace(ownerId), uuid);
        }

        protected virtual TaskPayload CreateWorkPayload(
            TimeSpan duration, string json, string ownerId, bool isRepeating)
        {
            return new TaskPayload
            {
                Uuid = Guid.NewGuid().ToString(),
                TaskName = taskName,
                OwnerId = ownerId,
                Duration = duration,
                IsRepeating = isRepeating,
                PayloadJson = json,
                StartTime = DateTime.UtcNow,
                TimeEpoch = DateTime.UtcNow + duration
            };
        }

        protected async Task QueueTaskAsync(TaskPayload payload)
        {
            await cacheClient.SortedSetUpsertAsync(
                parent.GetQueueNamespace(), payload.GetKey(), payload.TimeEpoch.Seconds);
            await cacheClient.HashUpsertAsync(
                parent.GetObjectNamespace(payload.OwnerId), payload.Uuid, payload);
        }
    }
}