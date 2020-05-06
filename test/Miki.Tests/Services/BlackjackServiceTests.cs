using System.Linq;
using System.Threading.Tasks;
using Miki.Cache.InMemory;
using Miki.Bot.Models;
using Miki.Services;
using Miki.Services.Transactions;
using Moq;
using Miki.Serialization.Protobuf;
using Xunit;

namespace Miki.Tests.Services
{
    public class BlackjackServiceTests : BaseEntityTest<MikiDbContext>
    {
        public BlackjackService Service { get; }

        public const ulong ValidUser = 1UL;

        public BlackjackServiceTests()
            : base(x => new MikiDbContext(x))
        {
            var cache = new InMemoryCacheClient(
                new ProtobufSerializer());
            var mock = Mock.Of<ITransactionService>();
            Service = new BlackjackService(cache, mock);
        }

        [Fact]
        public async Task CreateSession()
        {
            var session = await Service.NewSessionAsync(2UL, ValidUser, 0UL, 10);
            Assert.Equal(52, session.Deck.Count);
            Assert.Equal(2, session.Players.Count);
            Assert.Equal(10, session.Bet);
        }

        [Fact]
        public async Task CreateDuplicateSession()
        {
            await Service.NewSessionAsync(2UL, ValidUser, 0UL, 10);
            await Assext.ThrowsRootAsync<DuplicateSessionException>(
                () => Service.NewSessionAsync(2UL, ValidUser, 0UL, 10));
        }

        [Fact]
        public async Task DrawCard()
        {
            var session = await Service.NewSessionAsync(2UL, ValidUser, 0UL, 0);

            Service.DrawCard(session, ValidUser);

            Assert.NotEmpty(session.Players[ValidUser].Hand);
            Assert.DoesNotContain(session.Players[ValidUser].Hand.First(), session.Deck);
        }

        [Fact]
        public async Task LoadSession()
        {
            // values are 1UL because 0UL is a reserved value for the dealer hand.
            await Service.NewSessionAsync(1UL, ValidUser, 1UL, 100);

            var session = await Service.LoadSessionAsync(ValidUser, 1UL);
            Assert.NotNull(session);
            Assert.Equal(100, session.Bet);
        }

        [Fact]
        public async Task LoadNullSession()
        {
            await Assext.ThrowsRootAsync<BlackjackSessionNullException>(
                () => Service.LoadSessionAsync(ValidUser, 1UL));
        }
    }
}
