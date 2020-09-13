using System.Linq;
using System.Threading.Tasks;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest.Exceptions;
using Miki.Framework;
using Miki.Logging;
using Miki.Modules.Accounts.Services;
using Miki.Services.Achievements;
using Miki.Services.Transactions;
using Miki.Utility;

namespace Miki.Services.Lottery
{
    public class LotteryEventHandler
    {
        public string LotteryObjectsKey => "lottery:entries";

        public async Task HandleLotteryAsync(IContext context, string _)
        {
            var lotteryService = context.GetService<ILotteryService>();

            var entries = await lotteryService.GetEntriesAsync();
            var entryCount = entries?.Sum(x => x.TicketCount) ?? 0;
            if (entryCount == 0)
            {
                Log.Warning("No entries found.");
                return;
            }

            var winnerIndex = MikiRandom.Next(entryCount);

            LotteryEntry winner = null;
            foreach (var entry in entries)
            {
                if (entry.TicketCount > winnerIndex)
                {
                    winner = entry;
                    break;
                }

                winnerIndex -= entry.TicketCount;
            }

            if (winner == null)
            {
                Log.Warning("Winner was null");
                return;
            }

            var winningAmount = await lotteryService.GetTotalPrizeAsync();

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
            catch (DiscordRestException)
            {
                Log.Warning("Message failed to send");
                // Couldn't send message to winner.
            }

            var cache = context.GetService<ICacheClient>();
            await cache.RemoveAsync(LotteryObjectsKey);

            await OnLotteryWinAchievementsAsync(context, winner.UserId, winningAmount);
        }

        async Task OnLotteryWinAchievementsAsync(IContext context, long winnerUserId, int winningAmount)
        {
            var achievementService = context.GetService<AchievementService>();
            var lotteryAchievements = achievementService.GetAchievement(AchievementIds.LotteryWinId);
            if (winningAmount >= 100000)
            {
                await achievementService.UnlockAsync(
                   lotteryAchievements, (ulong)winnerUserId, 0);
            }
            if (winningAmount >= 10000000)
            {
                await achievementService.UnlockAsync(
                    lotteryAchievements, (ulong)winnerUserId, 1);
            }
            if (winningAmount >= 250000000)
            {
                await achievementService.UnlockAsync(
                    lotteryAchievements, (ulong)winnerUserId, 2);
            }

        }
    }
}
