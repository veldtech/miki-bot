namespace Miki.Tests.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Miki.Cache.InMemory;
    using Miki.Serialization.Protobuf;
    using Miki.Services.Scheduling;
    using Xunit;

    public class SchedulerServiceTests
    {
        private readonly ISchedulerService service;

        public SchedulerServiceTests()
        {
            service = new SchedulerService(null, new InMemoryCacheClient(new ProtobufSerializer()), null);
        }

        [Fact]
        public async Task TestScheduleSingleWorkerFlowAsync()
        {
            var asyncLock = new Semaphore(1, 1);
            var worker = service.CreateWorker("test", (e, json) =>
            {
                asyncLock.Release();
                Assert.Equal("test", json);
                return Task.CompletedTask;
            });

            await worker.QueueTaskAsync(TimeSpan.FromSeconds(1), "test", "1234", false);
            asyncLock.WaitOne(10000);
        }

        [Fact]
        public async Task TestScheduleGroupWorkerFlowAsync()
        {
            var asyncLock = new Semaphore(1, 1);
            var worker = service.CreateWorkerGroup("test", (e, json) =>
            {
                asyncLock.Release();
                Assert.Equal("test", json);
                return Task.CompletedTask;
            });

            await worker.QueueTaskAsync(
                TimeSpan.FromSeconds(1), "1234", "1234", "test", false);
            asyncLock.WaitOne(10000);
        }
    }
}
