namespace Miki.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Cache;
    using Cache.InMemory;
    using Miki.Services.Blackjack;
    using Patterns.Repositories;
    using Serialization.Protobuf;
    using Xunit;

    public class BlackjackServiceTests
    {
        public IAsyncRepository<BlackjackContext> Repository { get; }
        public BlackjackService Service { get; }

        public BlackjackServiceTests()
        {
            var cache = new InMemoryCacheClient(
                new ProtobufSerializer());
            Repository = new BlackjackRepository(cache);
            Service = new BlackjackService(Repository);
        }

        [Fact]
        public async Task CreateSession()
        {
            var session = await Service.CreateNewAsync(
                0L, 1L, 0L, 0);
            var context = await Repository.GetAsync(0, 0);
            Assert.Equal(session, new BlackjackSession(context));
        }
    }
}
