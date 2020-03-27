namespace Miki.Services.Scheduling
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Miki.Cache;
    using Miki.Cache.StackExchange;
    using Miki.Functional;
    using Miki.Logging;
    using Miki.Serialization;
    using StackExchange.Redis;

    public interface ISchedulerService
    {
        IScheduleWorker CreateWorker(Required<string> taskName, Func<string, Task> handler);

        IScheduleWorkerGroup CreateWorkerGroup(Required<string> taskName, Func<string, Task> handler);
    }
    public class SchedulerService : ISchedulerService, IAsyncDisposable
    {
        private const string SchedulerObjectsKey = "scheduler-objects";

        private readonly Task workerTask;
        private readonly StackExchangeCacheClient cacheClient;
        private readonly ISerializer serializer;
        private readonly IDictionary<string, Func<string, Task>> taskCallbacks;

        public SchedulerService(ICacheClient cacheClient, ISerializer serializer)
        {
            // TODO: write a better api for caching sorted lists
            if(!(cacheClient is StackExchangeCacheClient redisApi))
            {
                Log.Warning(
                    "Scheduler has been disabled due to compatibility issues outside of redis.");
                return;
            }

            this.serializer = serializer;
            this.cacheClient = redisApi;
            
            taskCallbacks = new Dictionary<string, Func<string, Task>>();
            workerTask = RunWorkerAsync();
        }

        public IScheduleWorker CreateWorker(
            Required<string> taskName,
            Func<string, Task> handler)
        {
            if(workerTask == null)
            {
                return null;
            }

            taskCallbacks.Add(taskName, handler);
            return new RedisScheduleWorker(taskName, cacheClient, serializer);
        }

        public IScheduleWorkerGroup CreateWorkerGroup(
            Required<string> taskName,
            Func<string, Task> handler)
        {
            if(workerTask == null)
            {
                return null;
            }

            taskCallbacks.Add(taskName, handler);
            return new RedisScheduleWorkerGroup(taskName, cacheClient, serializer);
        }

        private async Task<WorkPayload> FetchLatestWorkAsync()
        {
            try
            {
                var latestObject = await cacheClient.Client.GetDatabase()
                    .SortedSetPopAsync(SchedulerObjectsKey);
                if(!latestObject.HasValue)
                {
                    return null;
                }
                return serializer.Deserialize<WorkPayload>(latestObject.Value.Element);
            }
            catch
            {
                return null;
            }
        }

        private async Task RequeueWorkAsync(WorkPayload payload)
        {
            await cacheClient.Client.GetDatabase()
                .SortedSetAddAsync(
                    SchedulerObjectsKey, serializer.Serialize(payload), payload.TimeEpoch.Seconds);
        }
 
        private async Task RunWorkerAsync()
        {
            while(true)
            {
                await Task.Delay(1000);
                var payload = await FetchLatestWorkAsync();
                if(payload == null)
                {
                    continue;
                }

                if(payload.GetTimeRemaining().TotalSeconds > 0)
                {
                    await RequeueWorkAsync(payload);
                    continue;
                }

                if(!taskCallbacks.TryGetValue(payload.TaskName, out var worker))
                {
                    continue;
                }

                await worker(payload.PayloadJson);
            }
        }

        public class RedisScheduleWorker : IScheduleWorker
        {
            protected readonly string taskName;
            protected readonly StackExchangeCacheClient cacheClient;
            protected readonly ISerializer serializer;

            public RedisScheduleWorker(
                string taskName, StackExchangeCacheClient cacheClient, ISerializer serializer)
            {
                this.taskName = taskName;
                this.cacheClient = cacheClient;
                this.serializer = serializer;
            }

            /// <inheritdoc />
            public virtual async Task QueueTaskAsync(TimeSpan duration, string json)
            {
                await QueueTaskAsync(new WorkPayload
                {
                    TaskName = taskName,
                    Duration = duration,
                    Id = taskName,
                    IsRepeating = false,
                    PayloadJson = json,
                    StartTime = DateTime.UtcNow,
                    TimeEpoch = (DateTime.UtcNow + duration)
                });
            }

            protected async Task QueueTaskAsync(WorkPayload payload)
            {
                await cacheClient.Client.GetDatabase()
                    .SortedSetAddAsync(
                        SchedulerObjectsKey, 
                        serializer.Serialize(payload), 
                        payload.TimeEpoch.Seconds);
            }
        }

        public class RedisScheduleWorkerGroup : RedisScheduleWorker, IScheduleWorkerGroup
        {
            public RedisScheduleWorkerGroup(
                string taskName, StackExchangeCacheClient cacheClient, ISerializer serializer)
                : base(taskName, cacheClient, serializer)
            {
            }

            public override Task QueueTaskAsync(TimeSpan duration, string json)
            {
                return QueueTaskAsync(new WorkPayload
                {
                    TaskName = taskName,
                    Duration = duration,
                    Id = Guid.NewGuid().ToString(),
                    IsRepeating = false,
                    PayloadJson = json,
                    StartTime = DateTime.UtcNow,
                    TimeEpoch = (DateTime.UtcNow + duration)
                });
            }

            /// <inheritdoc />
            public Task<IEnumerable<WorkPayload>> GetQueuedWorkAsync()
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
        }
    }
}
