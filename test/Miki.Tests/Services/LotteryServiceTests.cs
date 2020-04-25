namespace Miki.Tests.Services
{
    using System.Threading.Tasks;
    using Miki.Bot.Models;
    using Miki.Discord.Common;
    using Miki.Services;
    using Miki.Services.Lottery;
    using Miki.Services.Scheduling;
    using Miki.Services.Transactions;
    using Moq;
    using Xunit;

    public class LotteryServiceTests : BaseEntityTest
    {
        public LotteryServiceTests()
        {
            using var context = NewDbContext();
            
            context.Users.Add(new User
            {
                Id = AppProps.Currency.BankId
            });

            context.Users.Add(new User
            {
                Id = 2L,
                Currency = 100
            });

            context.SaveChanges();
        }

        [Fact]
        public async Task WhenUserBuysTicketsShouldNotCrashAsync()
        {
            await using(var context = NewContext())
            {
                var userService = new UserService(context, CacheClient);
                var transactionService = new TransactionService(userService);
                var schedulerService = new SchedulerService(null, CacheClient, null);
                var discordClient = new Mock<IDiscordClient>();
                var lotteryService = new LotteryService(
                    CacheClient, schedulerService, transactionService, new LotteryEventHandler());

                await lotteryService.PurchaseEntriesAsync(2L, 1);
            }

            await using(var context = NewContext())
            {
                var userService = new UserService(context, CacheClient);
                var transactionService = new TransactionService(userService);
                var schedulerService = new SchedulerService(null, CacheClient, null);
                var discordClient = new Mock<IDiscordClient>();
                var lotteryService = new LotteryService(
                    CacheClient, schedulerService, transactionService, new LotteryEventHandler());

                var user = await userService.GetUserAsync(2L);
                var userEntries = await lotteryService.GetEntriesForUserAsync(2L);

                Assert.True(userEntries.IsValid);
                Assert.Equal(0, user.Currency);
                Assert.Equal(1, userEntries.Unwrap().TicketCount);
            }
        }
    }
}