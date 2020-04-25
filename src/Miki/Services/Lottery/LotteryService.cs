namespace Miki.Services.Lottery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Miki.Bot.Models.Exceptions;
    using Miki.Cache;
    using Miki.Cache.Extensions;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Rest.Exceptions;
    using Miki.Functional;
    using Miki.Logging;
    using Miki.Services.Scheduling;
    using Miki.Services.Transactions;
    using Miki.Utility;

    public class LotteryService : ILotteryService
    {
        private readonly ITransactionService transactions;
        private readonly IDiscordClient discordClient;
        private readonly IExtendedCacheClient cache;
        private readonly IHashSet<LotteryEntry> entrySet;
        private readonly IScheduleWorker scheduler;

        private const string lotterySchedulerKey = "schedule:lottery";
        private const string lotteryObjectsKey = "lottery:entries";
        private const string lotteryPayloadId = "lottery:uuid";
        private const string lotteryOwnerId = "lottery:owner";

        public int EntryPrice => 100;
        private int WinningAmount => 80;

        public LotteryService(
            IExtendedCacheClient cache, 
            ISchedulerService scheduler, 
            ITransactionService transactions, 
            IDiscordClient discordClient)
        {
            this.transactions = transactions;
            this.discordClient = discordClient;
            this.scheduler = scheduler.CreateWorker(lotterySchedulerKey, HandleLotteryAsync);
            this.cache = cache;
            this.entrySet = cache.CreateHashSet<LotteryEntry>(lotteryObjectsKey);
        }

        private async Task HandleLotteryAsync(string json)
        {
            var entries = await GetEntriesAsync();
            var entryCount = entries?.Sum(x => x.TicketCount) ?? 0;
            if(entryCount == 0)
            {
                return;
            }

            var winnerIndex = MikiRandom.Next(entryCount);
            
            LotteryEntry winner = null;
            foreach(var entry in entries)
            {
                if(entry.TicketCount > winnerIndex)
                {
                    winner = entry;
                    break;
                }

                winnerIndex -= entry.TicketCount;
            }

            if(winner == null)
            {
                Log.Warning("Winner was null");
                return;
            }

            var winningAmount = entryCount * WinningAmount;

            await transactions.CreateTransactionAsync(
                new TransactionRequest.Builder()
                    .WithSender(AppProps.Currency.BankId)
                    .WithReceiver(winner.UserId)
                    .WithAmount(winningAmount)
                    .Build());

            try
            {
                var channel = await discordClient.CreateDMAsync((ulong)winner.UserId);
                await channel.SendMessageAsync(
                    string.Empty, embed: new EmbedBuilder()
                        .SetTitle("🏅  Winner")
                        .SetDescription($"You won the jackpot of {winningAmount} mekos!")
                        .SetColor(103, 172, 237)
                        .ToEmbed());
            }
            catch(DiscordRestException)
            {
                // Couldn't send message to winner.
            }

            await cache.RemoveAsync(lotteryObjectsKey);
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

        private async ValueTask<IEnumerable<LotteryEntry>> GetEntriesAsync()
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
            return (await GetEntriesAsync()).Sum(x => x.TicketCount) * WinningAmount;
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
