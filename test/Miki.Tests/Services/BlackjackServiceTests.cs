namespace Miki.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            var session = await Service.NewSessionAsync(2UL, 1UL, 0UL, 0);
            var context = await Repository.GetAsync(0UL, 1UL);

            Assert.Equal(session.Deck.Count, context.Deck.Count);
            Assert.Equal(session.Players.Count, context.Hands.Count);
            Assert.Equal(session.Bet, context.Bet);
            Assert.Equal(
                session.GetHandWorth(session.Players[0]),
                session.GetHandWorth(context.Hands[0]));
        }

        [Fact]
        public async Task DrawCard()
        {
            var session = await Service.NewSessionAsync(2UL, 1UL, 0UL, 0);

            Service.DrawCard(session, 1UL);

            Assert.NotEmpty(session.Players[1UL].Hand);
            Assert.DoesNotContain(session.Players[1UL].Hand.First(), session.Deck);
        }
    }
}
