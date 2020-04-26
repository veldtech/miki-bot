namespace Miki.Services.Scheduling
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Miki.Cache;
    using Miki.Framework;
    using Miki.Functional;
    using Miki.Logging;
    using Sentry;

    public class SchedulerService : ISchedulerService, IDisposable, IAsyncDisposable
    {
        private const string SchedulerQueueKey = "scheduler-queue";
        private const string SchedulerObjectsKey = "scheduler-objects";

        private readonly ConfiguredTaskAwaitable workerTask;
        private readonly CancellationTokenSource cancellationToken;
        private readonly MikiApp app;

        private readonly IExtendedCacheClient cacheClient;
        private readonly ISentryClient sentryClient;
        private readonly IDictionary<string, Func<IContext, string, Task>> taskCallbacks;

        public SchedulerService(MikiApp app, IExtendedCacheClient cacheClient, ISentryClient sentryClient)
        {
            this.app = app;
            this.cacheClient = cacheClient;
            this.sentryClient = sentryClient;
            taskCallbacks = new Dictionary<string, Func<IContext, string, Task>>();
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
            Func<IContext, string, Task> handler)
        {
            if(taskCallbacks.ContainsKey(taskName))
            {
                return new ScheduleWorker(taskName, this, cacheClient);
            }

            taskCallbacks.Add(taskName, handler);
            return new ScheduleWorker(taskName, this, cacheClient);
        }

        public IScheduleWorkerGroup CreateWorkerGroup(
            Required<string> taskName,
            Func<IContext, string, Task> handler)
        {
            if(taskCallbacks.ContainsKey(taskName))
            {
                return new ScheduleWorkerGroup(taskName, this, cacheClient);
            }

            taskCallbacks.Add(taskName, handler);
            return new ScheduleWorkerGroup(taskName, this, cacheClient);
        }

        private async Task RequeueWorkAsync(TaskPayload payload, bool updatePayload)
        {
            if(updatePayload)
            {
                 await cacheClient.HashUpsertAsync(
                    GetObjectNamespace(payload.OwnerId), payload.Uuid, payload);
            }
            await cacheClient.SortedSetUpsertAsync(
                SchedulerQueueKey, payload.GetKey(), payload.TimeEpoch.Seconds);
        }

        private async Task<TaskPayload> FetchLatestWorkAsync()
        {
            var key = await cacheClient.SortedSetPopAsync<TaskKey>(SchedulerQueueKey);
            if(key == null || string.IsNullOrWhiteSpace(key?.Uuid))
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
                    await Task.Delay(1000, token).ConfigureAwait(false);
                }
                catch(TaskCanceledException)
                {
                    break;
                }

                var payload = await FetchLatestWorkAsync().ConfigureAwait(false);
                if(payload == null)
                {
                    Log.Warning("task was not valid.");
                    continue;
                }

                if(payload.GetTimeRemaining().TotalSeconds > 0)
                {
                    await RequeueWorkAsync(payload, false).ConfigureAwait(false);
                    continue;
                }

                if(!taskCallbacks.TryGetValue(payload.TaskName, out var worker))
                {
                    Log.Warning($"task '{payload.TaskName}' was not registerd with a proper handler.");
                    continue;
                }

                try
                {
                    using var context = new ContextObject(app.Services);
                    await worker(context, payload.PayloadJson).ConfigureAwait(false);
                }
                catch(Exception e)
                {
                    Log.Error(e);
                    sentryClient?.CaptureException(e);
                }

                if(payload.IsRepeating)
                {
                    payload.StartTime = DateTime.UtcNow;
                    payload.TimeEpoch = payload.StartTime.Add(payload.Duration);
                    await RequeueWorkAsync(payload, true).ConfigureAwait(false);
                }
                else
                {
                    await DeletePayloadAsync(payload).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            cancellationToken.Cancel();
            await workerTask;
            Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            cancellationToken?.Dispose();
        }
    }
}

