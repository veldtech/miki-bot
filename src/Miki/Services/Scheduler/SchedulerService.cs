namespace Miki.Services.Scheduler
{
    using System;
    using Framework;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Cache;

    public class SchedulerService : ISchedulerService
    {
        private readonly ICacheClient cacheClient;

        private List<ScheduledTask> scheduledTasks = new List<ScheduledTask>();
        private Dictionary<string, List<ScheduledTaskGroup>> scheduledTaskGroups = new Dictionary<string, List<ScheduledTaskGroup>>();

        private Thread schedulerThread;

        private const bool RunThread = true;

        public SchedulerService(IUnitOfWork unitOfWork, ICacheClient cacheClient)
        {
            this.cacheClient = cacheClient;

            schedulerThread = new Thread(ThreadUpdate);
            schedulerThread.Start();
        }

        private void ThreadUpdate()
        {
            while (RunThread)
            {
                // Process queued events here.
                Thread.Sleep(1000);
            }
        }

        public async ValueTask<ScheduledTask> SetupTask()
        {
            // Setup task and return an object to identify the job.
            return new ScheduledTask();
        }

        public void Dispose()
        {
            
        }
    }

    public interface ISchedulerService : IDisposable
    {
        ValueTask<ScheduledTask> SetupTask();
    }
}
