using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Miki.Bot.Models.Exceptions;
using Miki.Cache;
using Miki.Cache.Extensions;
using Miki.Functional;
using Miki.Services.Scheduling;
using Miki.Services.Transactions;

namespace Miki.Services.Lottery
{
    public class LotteryService : ILotteryService
    {
        private readonly ITransactionService transactions;
        private readonly IHashSet<LotteryEntry> entrySet;
        private readonly IScheduleWorker scheduler;

        private const string lotterySchedulerKey = "schedule:lottery";
        private const string lotteryPayloadId = "lottery:uuid";
        private const string lotteryOwnerId = "lottery:owner";

        public int EntryPrice => 100;
        public int WinningAmount => 80;
        private int UpfrontPrize => 10000;
        
        public LotteryService(
            IExtendedCacheClient cache, 
            ISchedulerService scheduler, 
            ITransactionService transactions, 
            LotteryEventHandler eventHandler)
        {
            this.transactions = transactions;
            this.scheduler = scheduler.CreateWorker(
                lotterySchedulerKey, eventHandler.HandleLotteryAsync);
            this.entrySet = cache.CreateHashSet<LotteryEntry>(eventHandler.LotteryObjectsKey);
        }

        public async ValueTask<Result<LotteryEntry>> GetEntriesForUserAsync(long userId)
        {
            var entries = await entrySet.GetAsync(userId.ToString());
            if(entries == null)
            {
                return new EntityNullException<LotteryEntry>();
            }
            return entries;
        }

        public async ValueTask<IEnumerable<LotteryEntry>> GetEntriesAsync()
        {
            return await entrySet.ValuesAsync();
        }

        public async ValueTask<LotteryEntry> PurchaseEntriesAsync(
            long userId, int amountOfTickets)
        {
            var entries = (await GetEntriesForUserAsync(userId))
                .OrElse(new LotteryEntry
                {
                    UserId = userId
                })
                .Unwrap();

            await transactions.CreateTransactionAsync(
                new TransactionRequest.Builder()
                    .WithAmount(EntryPrice * amountOfTickets)
                    .WithReceiver(AppProps.Currency.BankId)
                    .WithSender(userId)
                    .Build());

            entries.TicketCount += amountOfTickets;
            await SaveEntriesAsync(entries);
            return entries;
        }

        public async ValueTask<int> GetTotalPrizeAsync()
        {
            return UpfrontPrize + (await GetEntriesAsync()).Sum(x => x.TicketCount) * WinningAmount;
        }

        public async ValueTask<TaskPayload> GetLotteryTaskAsync()
        {
            var payload = await scheduler.GetTaskAsync(lotteryOwnerId, lotteryPayloadId);
            if(payload == null)
            {
                return await scheduler.QueueTaskAsync(
                    TimeSpan.FromHours(1), lotteryPayloadId, lotteryOwnerId, string.Empty, true);
            }

            return payload;
        }

        private async ValueTask SaveEntriesAsync(LotteryEntry entry)
        {
            await entrySet.AddAsync(entry.UserId.ToString(), entry);
        }
    }
}
