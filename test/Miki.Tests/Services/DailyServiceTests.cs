namespace Miki.Tests.Services
{
    using System;
    using System.Threading.Tasks;
    using Miki.Bot.Models;
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
            ctx.Set<Daily>().Add(new Daily
            {
                UserId = 5,
                CurrentStreak = 240,
                LongestStreak = 240,
                LastClaimTime = DateTime.UtcNow.AddDays(-1)
            });
            ctx.SaveChanges();
        }

        [Fact]
        public async Task ClaimDailyTest()
        {
            await using var unit = NewContext();
            var userService = new UserService(unit);
            var transactionService = new TransactionService(userService);
            var dailyService = new DailyService(unit, userService, transactionService);

            var response = await dailyService.ClaimDailyAsync(2L);

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
            await using var unit = NewContext();
            var userService = new UserService(unit);
            var transactionService = new TransactionService(userService);
            var dailyService = new DailyService(unit, userService, transactionService);
            
            var response = await dailyService.ClaimDailyAsync(3L);
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
            await using var unit = NewContext();
            var userService = new UserService(unit);
            var transactionService = new TransactionService(userService);
            var dailyService = new DailyService(unit, userService, transactionService);

            await dailyService.ClaimDailyAsync(2L);

            var daily = await dailyService.GetOrCreateDailyAsync(2L);

            Assert.NotNull(daily);
            Assert.Equal(1, daily.CurrentStreak);
            Assert.Equal(1, daily.LongestStreak);
            Assert.InRange(daily.LastClaimTime, DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddMinutes(2));
        }

        [Fact]
        public async Task DailyStreakResetTest()
        {
            await using var unit = NewContext();
            var userService = new UserService(unit);
            var transactionService = new TransactionService(userService);
            var dailyService = new DailyService(unit, userService, transactionService);

            await dailyService.ClaimDailyAsync(4L);

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
        public async Task HigherThanHundredDailyTest()
        {
            await using var unit = NewContext();
            
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserAsync(It.IsAny<long>()))
                .Returns(new ValueTask<User>(new User {Id = 5L}));
            var transactionServiceMock = new Mock<ITransactionService>();
            transactionServiceMock.Setup(x => x.CreateTransactionAsync(It.IsAny<TransactionRequest>()))
                .Returns(Task.FromResult(new TransactionResponse(null, null, 0)));
            
            var dailyService = new DailyService(
                unit, userServiceMock.Object, transactionServiceMock.Object);

            var dailyResponse = await dailyService.ClaimDailyAsync(5L);
            Assert.Equal(2100, dailyResponse.AmountClaimed);
        }
    }
}
