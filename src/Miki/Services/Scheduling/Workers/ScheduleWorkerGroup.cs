namespace Miki.Services.Scheduling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Miki.Cache;
    using Miki.Functional;

    public class ScheduleWorkerGroup : ScheduleWorker, IScheduleWorkerGroup
    {
        public ScheduleWorkerGroup(
            string taskName, ISchedulerService parent, IExtendedCacheClient cacheClient)
            : base(taskName, parent, cacheClient)
        {
        }

        protected override TaskPayload CreateWorkPayload(
            TimeSpan duration, string json, string uuid, string ownerId, bool isRepeating)
        {
            var workPayload = base.CreateWorkPayload(duration, json, uuid, ownerId, isRepeating);
            workPayload.TaskId = Guid.NewGuid().ToString();
            return workPayload;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<TaskPayload>> GetQueuedWorkAsync(string ownerId)
        {
            var result = await cacheClient
                .HashGetAllAsync<TaskPayload>(parent.GetObjectNamespace(ownerId));
            return result?.Select(x => x.Value).ToList();
        }
    }
}