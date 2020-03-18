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

    public class BankAccountServiceTests : BaseEntityTest<MikiDbContext>
    {
        /// <inheritdoc />
        public BankAccountServiceTests()
            : base(x => new MikiDbContext(x))
        {
            using var ctx = NewDbContext();
            ctx.Set<BankAccount>().Add(new BankAccount
            {
                UserId = 1,
                GuildId = 1,
                Currency = 5000,
                TotalDeposited = 3000
            });
            ctx.SaveChanges();
        }

        [Fact]
        public async Task DepositExistingAccountTest()
        {
            await using var unit = NewContext();
            var accountService = new BankAccountService(unit);

            await accountService.DepositAsync(1L, 1L, 5000);

            var account = await accountService.GetOrCreateAccountAsync(1L, 1L);

            Assert.NotNull(account);
            Assert.Equal(10000, account.Currency);
            Assert.Equal(8000, account.TotalDeposited);
        }

        [Fact]
        public async Task DepositNonExistingAccountTest()
        {
            await using var unit = NewContext();
            var accountService = new BankAccountService(unit);

            await accountService.DepositAsync(1L, 2L, 5000);

            var account = await accountService.GetOrCreateAccountAsync(1L, 2L);

            Assert.NotNull(account);
            Assert.Equal(5000, account.Currency);
            Assert.Equal(5000, account.TotalDeposited);
        }
    }
}
