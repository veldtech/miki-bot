namespace Miki.Services
{
    using System;
    using System.Threading.Tasks;
    using Bot.Models;
    using Framework;
    using Patterns.Repositories;

    public class BankAccountService : IBankAccountService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IAsyncRepository<BankAccount> repository;

        public BankAccountService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            this.repository = unitOfWork.GetRepository<BankAccount>();
        }

        /// <inheritdoc />
        public async ValueTask<BankAccount> CreateAccountAsync(AccountDetails accountDetails)
        {
            var account = new BankAccount
            {
                UserId = accountDetails.UserId,
                GuildId = accountDetails.GuildId
            };
            await repository.AddAsync(account);
            await unitOfWork.CommitAsync();
            return account;
        }

        /// <inheritdoc />
        public async ValueTask<BankAccount> GetAccountAsync(AccountDetails accountDetails)
        {
            var account = await repository.GetAsync(accountDetails.UserId, accountDetails.GuildId);
            if (account == null)
            {
                throw new BankAccountNullException();
            }
            return account;
        }

        /// <inheritdoc />
        public async ValueTask<BankAccount> DepositAsync(AccountDetails accountDetails, int amount)
        {
            var account = await this.GetOrCreateBankAccountAsync(accountDetails).ConfigureAwait(false);

            account.Currency += amount;
            account.TotalDeposited += amount;

            await UpdateAccountAsync(account).ConfigureAwait(false);
            await SaveAsync().ConfigureAwait(false);

            return account;
        }

        // TODO: Add withdraw capability.
        /// <inheritdoc />
        public ValueTask<BankAccount> WithdrawAsync(AccountDetails accountDetails, int amount)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public ValueTask UpdateAccountAsync(BankAccount account)
            => repository.EditAsync(account);

        /// <inheritdoc />
        public ValueTask SaveAsync()
            => unitOfWork.CommitAsync();

        /// <inheritdoc />
        public void Dispose()
            => unitOfWork?.Dispose();
    }

    public struct AccountDetails
    {
        public long UserId;
        public long GuildId;

        public AccountDetails(long userId = 1, long guildId = 1)
        {
            UserId = userId;
            GuildId = guildId;
        }
    }

    public interface IBankAccountService : IDisposable
    {
        ValueTask<BankAccount> CreateAccountAsync(AccountDetails accountDetails);

        ValueTask<BankAccount> GetAccountAsync(AccountDetails accountDetails);

        ValueTask<BankAccount> DepositAsync(AccountDetails accountDetails, int amount);

        ValueTask<BankAccount> WithdrawAsync(AccountDetails accountDetails, int amount);

        ValueTask UpdateAccountAsync(BankAccount account);

        ValueTask SaveAsync();
    }
}
