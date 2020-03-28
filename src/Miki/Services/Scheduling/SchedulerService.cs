namespace Miki.Services.Scheduling
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Miki.Cache;
    using Miki.Functional;
    using Miki.Logging;
    using Sentry;

    public class SchedulerService : ISchedulerService, IAsyncDisposable
    {
        private const string SchedulerQueueKey = "scheduler-queue";
        private const string SchedulerObjectsKey = "scheduler-objects";

        private readonly ConfiguredTaskAwaitable workerTask;
        private readonly CancellationTokenSource cancellationToken;

        private readonly IExtendedCacheClient cacheClient;
        private readonly ISentryClient sentryClient;
        private readonly IDictionary<string, Func<string, Task>> taskCallbacks;

        public SchedulerService(IExtendedCacheClient cacheClient, ISentryClient sentryClient)
        {
            this.cacheClient = cacheClient;
            this.sentryClient = sentryClient;
            taskCallbacks = new Dictionary<string, Func<string, Task>>();
            cancellationToken = new CancellationTokenSource();
            workerTask = RunWorkerAsync(cancellationToken.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Cache key to the current scheduled work tasks.
        /// </summary>
        /// <returns></returns>
        public string GetQueueNamespace()
        {
            return SchedulerQueueKey;
        }

        /// <summary>
        /// Decorates the base key with an owner, if the owner exists.
        /// </summary>
        /// <returns></returns>
        public string GetObjectNamespace(string ownerId)
        {
            if(string.IsNullOrWhiteSpace(ownerId))
            {
                return SchedulerObjectsKey;
            }
            return SchedulerObjectsKey + $":{ownerId}";
        }

        public IScheduleWorker CreateWorker(
            Required<string> taskName,
            Func<string, Task> handler)
        {
            taskCallbacks.Add(taskName, handler);
            return new ScheduleWorker(taskName, this, cacheClient);
        }

        public IScheduleWorkerGroup CreateWorkerGroup(
            Required<string> taskName,
            Func<string, Task> handler)
        {
            taskCallbacks.Add(taskName, handler);
            return new ScheduleWorkerGroup(taskName, this, cacheClient);
        }

        private async Task RequeueWorkAsync(TaskPayload payload)
        {
            await cacheClient.SortedSetUpsertAsync(
                SchedulerQueueKey, payload.GetKey(), payload.TimeEpoch.Seconds);
        }

        private async Task<TaskPayload> FetchLatestWorkAsync()
        {
            var key = await cacheClient.SortedSetPopAsync<TaskKey>(SchedulerQueueKey);
            if(string.IsNullOrWhiteSpace(key?.Uuid))
            {
                return null;
            }
            return await cacheClient.HashGetAsync<TaskPayload>(
                GetObjectNamespace(key.OwnerId), key.Uuid);
        }

        private async Task DeletePayloadAsync(TaskPayload payload)
        {
            await cacheClient.HashDeleteAsync(GetObjectNamespace(payload.OwnerId), payload.Uuid);
        }

        private async Task RunWorkerAsync(CancellationToken token)
        {
            while(true)
            {
                try
                {
                    await Task.Delay(1000, token);
                }
                catch(TaskCanceledException)
                {
                    break;
                }

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

                try
                {
                    await worker(payload.PayloadJson);
                }
                catch(Exception e)
                {
                    Log.Error(e);
                    sentryClient?.CaptureException(e);
                }

                await DeletePayloadAsync(payload);
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            cancellationToken.Cancel();
            await workerTask;
        }
    }
}

