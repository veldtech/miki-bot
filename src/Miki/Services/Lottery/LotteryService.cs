namespace Miki.Services.Lottery
{
    using System;
    using System.Threading.Tasks;
    using Miki.Bot.Models.Exceptions;
    using Miki.Cache;
    using Miki.Cache.Extensions;
    using Miki.Functional;
    using Miki.Services.Scheduling;
    using Miki.Services.Transactions;

    public class LotteryService
    {
        private readonly ITransactionService transactions;
        private readonly IHashSet<LotteryEntry> cache;
        private readonly IScheduleWorker scheduler;

        private const string lotterySchedulerKey = "schedule:lottery";
        private const string lotteryObjectsKey = "lottery:entries";

        public LotteryService(
            IExtendedCacheClient cache, ISchedulerService scheduler, ITransactionService transactions)
        {
            this.transactions = transactions;
            this.scheduler = scheduler.CreateWorker(lotterySchedulerKey, HandleLotteryAsync);
            this.cache = cache.CreateHashSet<LotteryEntry>(lotteryObjectsKey);
        }

        private Task HandleLotteryAsync(string json)
        {
            // TODO(Veld): handle scheduled lottery events
            return Task.CompletedTask;
        }

        public int GetEntryPrice()
        {
            return 100;
        }

        public async ValueTask<Result<LotteryEntry>> GetEntriesForUserAsync(long userId)
        {
            var entries = await cache.GetAsync(userId.ToString());
            if(entries == null)
            {
                return new EntityNullException<LotteryEntry>();
            }
            return entries;
        }

        public async ValueTask<Result<LotteryEntry>> PurchaseEntriesAsync(
            long userId,
            int amountOfTickets)
        {
            var getEntryResult = await GetEntriesForUserAsync(userId);
            var entries = getEntryResult
                .OrElse(
                    new LotteryEntry
                    {
                        UserId = userId,
                        TicketCount = 0
                    })
                .Unwrap();

            await transactions.CreateTransactionAsync(
                new TransactionRequest.Builder()
                    .WithAmount(GetEntryPrice() * amountOfTickets)
                    .WithReceiver(AppProps.Currency.BankId)
                    .WithSender(userId)
                    .Build());

            entries.TicketCount += amountOfTickets;
            await SaveEntriesAsync(entries);

            return entries;
        }

        private async ValueTask SaveEntriesAsync(LotteryEntry entry)
        {
            await cache.AddAsync(entry.UserId.ToString(), entry);
        }
    }
}
