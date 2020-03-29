namespace Miki.Services.Scheduling
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IScheduleWorkerGroup : IScheduleWorker
    {

        Task<IReadOnlyList<TaskPayload>> GetQueuedWorkAsync(string ownerId);
    }
}