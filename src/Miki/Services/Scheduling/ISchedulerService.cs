using System;
using System.Threading.Tasks;
using Miki.Framework;
using Miki.Functional;

namespace Miki.Services.Scheduling
{
    public interface ISchedulerService
    {
        IScheduleWorker CreateWorker(Required<string> taskName, Func<IContext, string, Task> handler);
        IScheduleWorkerGroup CreateWorkerGroup(Required<string> taskName, Func<IContext, string, Task> handler);
        string GetObjectNamespace(string ownerId);
        string GetQueueNamespace();
    }
}