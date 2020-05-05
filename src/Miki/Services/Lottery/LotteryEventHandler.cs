namespace Miki.Services.Lottery
{
    using System.Linq;
    using System.Threading.Tasks;
    using Miki.Cache;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Rest.Exceptions;
    using Miki.Framework;
    using Miki.Logging;
    using Miki.Services.Transactions;
    using Miki.Utility;

    public class LotteryEventHandler
    {
        public string LotteryObjectsKey => "lottery:entries";

        public async Task HandleLotteryAsync(IContext context, string _)
        {
            var lotteryService = context.GetService<ILotteryService>();

            var entries = await lotteryService.GetEntriesAsync();
            var entryCount = entries?.Sum(x => x.TicketCount) ?? 0;
            if(entryCount == 0)
            {
                Log.Warning("No entries found.");
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

            var winningAmount = entryCount * lotteryService.WinningAmount;

            var transactionService = context.GetService<ITransactionService>();
            await transactionService.CreateTransactionAsync(
                new TransactionRequest.Builder()
                    .WithSender(AppProps.Currency.BankId)
                    .WithReceiver(winner.UserId)
                    .WithAmount(winningAmount)
                    .Build());

            try
            {
                var discordClient = context.GetService<IDiscordClient>();
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
                Log.Warning("Message failed to send");
                // Couldn't send message to winner.
            }

            var cache = context.GetService<ICacheClient>();
            await cache.RemoveAsync(LotteryObjectsKey);
        }
    }
}
