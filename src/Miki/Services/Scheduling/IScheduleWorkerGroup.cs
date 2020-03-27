namespace Miki.Services.Scheduling
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IScheduleWorkerGroup : IScheduleWorker
    {
        Task<IEnumerable<WorkPayload>> GetQueuedWorkAsync();
    }
}