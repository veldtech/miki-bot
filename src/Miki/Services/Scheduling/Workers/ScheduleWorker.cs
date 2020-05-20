using System;
using System.Threading.Tasks;
using Miki.Cache;
using Miki.Functional;

namespace Miki.Services.Scheduling
{
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
        public async Task<TaskPayload> GetTaskAsync(string ownerId, string uuid)
        {
            return await cacheClient.HashGetAsync<TaskPayload>(
                parent.GetObjectNamespace(ownerId), uuid);
        }


        /// <inheritdoc />
        public async Task<TaskPayload> QueueTaskAsync(TimeSpan duration, string uuid, string ownerId, string json, bool isRepeating)
        {
            var payload = CreateWorkPayload(duration, json, uuid, ownerId, isRepeating);
            await QueueTaskAsync(payload);
            return payload;
        }

        /// <inheritdoc />
        public async Task CancelTaskAsync(string uuid, Optional<string> ownerId)
        {
            await cacheClient.HashDeleteAsync(parent.GetObjectNamespace(ownerId), uuid);
        }

        protected virtual TaskPayload CreateWorkPayload(
            TimeSpan duration, string json, string uuid, string ownerId, bool isRepeating)
        {
            return new TaskPayload
            {
                Uuid = uuid ?? Guid.NewGuid().ToString(),
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