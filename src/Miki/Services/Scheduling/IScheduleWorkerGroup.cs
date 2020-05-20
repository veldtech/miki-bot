using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.Services.Scheduling
{
    public interface IScheduleWorkerGroup : IScheduleWorker
    {

        Task<IReadOnlyList<TaskPayload>> GetQueuedWorkAsync(string ownerId);
    }
}