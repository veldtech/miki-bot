namespace Miki.Tests.Services
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Exceptions;
    using Miki.Services;
    using Miki.Services.Transactions;
    using Moq;
    using Xunit;
    public class TransactionServiceTests : BaseEntityTest<MikiDbContext>
    {
        /// <inheritdoc />
        public TransactionServiceTests()
            : base(options => new MikiDbContext(options))
        {
            using var ctx = NewDbContext();
            ctx.Users.Add(new User
            {
                Id = 1,
                Currency = 10
            });
            ctx.Users.Add(new User
            {
                Id = 2,
                Currency = 0
            });
            ctx.SaveChanges();
        }

        [Fact]
        public async Task TransferTest()    
        {
            await using(var unit = NewContext())
            {
                var userService = new UserService(unit, null);

                var service = new TransactionService(userService, null);
                await service.CreateTransactionAsync(new TransactionRequest(1L, 2L, 10));
            }

            await using(var unit = NewContext())
            {
                var userService = new UserService(unit, null);

                var user1 = await userService.GetUserAsync(1L);
                Assert.NotNull(user1);
                Assert.Equal(0, user1.Currency);

                var user2 = await userService.GetUserAsync(2L);
                Assert.NotNull(user2);
                Assert.Equal(10, user2.Currency);
            }
        }

        [Fact]
        public async Task InvalidCurrencyTransferTest()
        {
            var unit = NewContext();
            var userService = new UserService(unit, null);

            // TODO: replace with better testing solution.
            bool ranCallback = false;

            var events = new TransactionEvents();
            events.OnTransactionFailed += (a, b) =>
            {
                Assert.IsType<InsufficientCurrencyException>(b);
                ranCallback = true;
                return Task.CompletedTask;
            };

            var service = new TransactionService(userService, events);

            await Assext.ThrowsRootAsync<InsufficientCurrencyException>(
                async () => await service.CreateTransactionAsync(new TransactionRequest(2L, 1L, 10)));

            Assert.True(ranCallback, "TransactionService did not call error event");
        }

        [Fact]
        public async Task InvalidSelfTransferTest()
        {
            var unit = NewContext();
            var userService = new UserService(unit, null);

            var service = new TransactionService(userService);
            await Assext.ThrowsRootAsync<UserNullException>(
                async () => await service.CreateTransactionAsync(new TransactionRequest(1L, 1L, 10)));
        }
    }
}
