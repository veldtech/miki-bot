using Miki.Cache;
using Miki.Services.Scheduler;

namespace Miki.Services.Lottery
{
    using System;
    using System.Collections.Generic;
    using System.Text;


    public class LotteryService : ILotteryService
    {
        private ICacheClient cacheClient;
        private ISchedulerService scheduler;

        public LotteryService(ICacheClient cacheClient, ISchedulerService scheduler)
        {
            this.cacheClient = cacheClient;
            this.scheduler = scheduler;

            scheduler.SetupTask();
        }

        public void Dispose()
        {

        }
    }

    interface ILotteryService : IDisposable
    {
        
    }
}
