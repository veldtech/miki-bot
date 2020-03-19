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
            var accountDetails = new AccountDetails
            {
                UserId = 1,
                GuildId = 1
            };

            await accountService.DepositAsync(accountDetails, 5000);

            var account = await accountService.GetOrCreateBankAccountAsync(accountDetails);

            Assert.NotNull(account);
            Assert.Equal(10000, account.Currency);
            Assert.Equal(8000, account.TotalDeposited);
        }

        [Fact]
        public async Task DepositNonExistingAccountTest()
        {
            await using var unit = NewContext();
            var accountService = new BankAccountService(unit);
            var accountDetails = new AccountDetails
            {
                UserId = 1,
                GuildId = 2
            };

            await accountService.DepositAsync(accountDetails, 5000);

            var account = await accountService.GetOrCreateBankAccountAsync(accountDetails);

            Assert.NotNull(account);
            Assert.Equal(5000, account.Currency);
            Assert.Equal(5000, account.TotalDeposited);
        }
    }
}
