using Amazon.Runtime.Internal.Util;
using Miki.Framework;

namespace Miki.Tests.Services
{
    using System;
    using System.Threading.Tasks;
    using Miki.Bot.Models;
    using Miki.Cache;
    using Miki.Cache.InMemory;
    using Miki.Serialization.Protobuf;
    using Miki.Services;
    using Miki.Services.Daily;
    using Miki.Services.Transactions;
    using Xunit;
    using Moq;

    public class DailyServiceTest : BaseEntityTest<MikiDbContext>
    {
        /// <inheritdoc />
        public DailyServiceTest()
            : base(x => new MikiDbContext(x))
        {
            using var ctx = NewDbContext();
            ctx.Set<User>().Add(new User
            {
                Id = 1,
                Currency = 100000
            });
            ctx.Set<User>().Add(new User
            {
                Id = 2,
                Currency = 0
            });
            ctx.Set<User>().Add(new User
            {
                Id = 3,
                Currency = 0
            });
            ctx.Set<User>().Add(new User
            {
                Id = 4,
                Currency = 0
            });

            ctx.Set<IsDonator>().Add(new IsDonator
            {
                UserId = 3,
                ValidUntil = DateTime.UtcNow.AddYears(1000)

            });

            ctx.Set<Daily>().Add(new Daily
            {
                UserId = 2,
                CurrentStreak = 0,
                LongestStreak = 0,
                LastClaimTime = DateTime.UtcNow.AddDays(-1)
            });
            ctx.Set<Daily>().Add(new Daily
            {
                UserId = 3,
                CurrentStreak = 5,
                LongestStreak = 10,
                LastClaimTime = DateTime.UtcNow.AddDays(-1)
            });

            ctx.Set<Daily>().Add(new Daily
            {
                UserId = 4,
                CurrentStreak = 7,
                LongestStreak = 14,
                LastClaimTime = DateTime.UtcNow.AddDays(-3)
            });
            ctx.SaveChanges();
        }

        [Fact]
        public async Task ClaimDailyTest()
        {
            var testContext = new TestContextObject();
            testContext.SetService(typeof(ICacheClient),
                new InMemoryCacheClient(new ProtobufSerializer()));

            await using var unit = NewContext();
            var userService = new UserService(unit);
            var transactionService = new TransactionService(userService);
            var dailyService = new DailyService(unit, userService, transactionService);

            var response = await dailyService.ClaimDailyAsync(2L, testContext);

            Assert.NotNull(response);
            Assert.Equal(1, response.CurrentStreak);
            Assert.Equal(1, response.LongestStreak);
            Assert.Equal(120, response.AmountClaimed);
            Assert.InRange(
                response.LastClaimTime, DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddMinutes(2));
        }

        [Fact]
        public async Task DonatorClaimDailyTest()
        {
            var testContext = new TestContextObject();
            testContext.SetService(typeof(ICacheClient),
                new InMemoryCacheClient(new ProtobufSerializer()));

            await using var unit = NewContext();
            var userService = new UserService(unit);
            var transactionService = new TransactionService(userService);
            var dailyService = new DailyService(unit, userService, transactionService);
            
            var response = await dailyService.ClaimDailyAsync(3L, testContext);
            var expectedClaimAmount = (AppProps.Daily.DailyAmount + AppProps.Daily.StreakAmount * 6) * 2;

            Assert.NotNull(response);
            Assert.Equal(6, response.CurrentStreak);
            Assert.Equal(10, response.LongestStreak);
            Assert.Equal(expectedClaimAmount, response.AmountClaimed);
            Assert.InRange(response.LastClaimTime, DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddMinutes(2));
        }

        [Fact]
        public async Task CheckIfDailyUpdatedInDatabaseTest()
        {
            var testContext = new TestContextObject();
            testContext.SetService(typeof(ICacheClient),
                new InMemoryCacheClient(new ProtobufSerializer()));

            await using var unit = NewContext();
            var userService = new UserService(unit);
            var transactionService = new TransactionService(userService);
            var dailyService = new DailyService(unit, userService, transactionService);

            await dailyService.ClaimDailyAsync(2L, testContext);

            var daily = await dailyService.GetOrCreateDailyAsync(2L);

            Assert.NotNull(daily);
            Assert.Equal(1, daily.CurrentStreak);
            Assert.Equal(1, daily.LongestStreak);
            Assert.InRange(daily.LastClaimTime, DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddMinutes(2));
        }

        [Fact]
        public async Task DailyStreakResetTest()
        {
            var testContext = new TestContextObject();
            testContext.SetService(typeof(ICacheClient),
                new InMemoryCacheClient(new ProtobufSerializer()));

            await using var unit = NewContext();
            var userService = new UserService(unit);
            var transactionService = new TransactionService(userService);
            var dailyService = new DailyService(unit, userService, transactionService);

            await dailyService.ClaimDailyAsync(4L, testContext);

            var daily = await dailyService.GetOrCreateDailyAsync(4L);

            Assert.Equal(0, daily.CurrentStreak);
        }

        [Fact]
        public async Task GetOrCreateDailyTest()
        {
            await using var unit = NewContext();
            var userService = new UserService(unit);
            var transactionService = new TransactionService(userService);
            var dailyService = new DailyService(unit, userService, transactionService);

            var daily = await dailyService.GetOrCreateDailyAsync(2L);

            Assert.NotNull(daily);
        }

        [Fact]
        public async Task MigrateCacheToDatabaseTest()
        {
            await using var unit = NewContext();
            var userService = new UserService(unit);
            var transactionService = new TransactionService(userService);
            var dailyService = new DailyService(unit, userService, transactionService);

            var cacheMock = new Mock<ICacheClient>();
            cacheMock.Setup(x => x.ExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
            cacheMock.Setup(x => x.GetAsync<int>(It.IsAny<string>()))
                .ReturnsAsync(5);

            var contextMock = new TestContextObject();
            contextMock.SetService(typeof(ICacheClient), cacheMock.Object);

            await dailyService.ClaimDailyAsync(2L, contextMock);

            var daily = await dailyService.GetOrCreateDailyAsync(2L);

            Assert.Equal(6, daily.CurrentStreak);
        }
    }
}
