using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Miki.API.Cards.Objects;
using Miki.Bot.Models;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Localization;
using Miki.Modules.Accounts.Services;
using Miki.Modules.Gambling.Exceptions;
using Miki.Modules.Gambling.Resources;
using Miki.Services;
using Miki.Services.Achievements;
using Miki.Services.Lottery;
using Miki.Services.Rps;
using Miki.Services.Transactions;
using Miki.Utility;

namespace Miki.Modules.Gambling
{

    [Module("Gambling")]
    public class GamblingModule
    {
        [Command("rps")]
        public class RpsCommand
        {
            private const int MaxBet = 10000;

            [Command]
            public async Task RpsAsync(IContext e)
            {
                var locale = e.GetLocale();
                var userService = e.GetService<IUserService>();
                var rps = e.GetService<IRpsService>();

                User user = await userService.GetOrCreateUserAsync(e.GetAuthor())
                    .ConfigureAwait(false);

                int bet = ValidateBet(e, user, MaxBet);

                var weapon = e.GetArgumentPack().TakeRequired<string>("noun_weapon");
                var result = await rps.PlayRpsAsync((long)e.GetAuthor().Id, bet, weapon);

                EmbedBuilder resultMessage = new EmbedBuilder()
                    .SetTitle("Rock, Paper, Scissors!")
                    .SetDescription(
                        $"{result.PlayerWeapon.Name.ToUpper()} {result.PlayerWeapon.Emoji} vs. " 
                        + $"{result.CpuWeapon.Emoji} {result.CpuWeapon.Name.ToUpper()}");

                resultMessage.Description += "\n\n" + locale.GetString(
                    new GameResultResource(result.Status, user.Currency, bet, result.AmountWon ?? 0));
                await resultMessage.ToEmbed().QueueAsync(e, e.GetChannel()).ConfigureAwait(false);
            }
        }

        [Command("blackjack", "bj")]
        public class BlackjackCommand
        {
            [Command]
            public async Task BlackjackAsync(IContext e)
            {
                await new EmbedBuilder()
                    .SetTitle("🎲 Blackjack")
                    .SetColor(234, 89, 110)
                    .SetDescription("Play a game of blackjack against miki!\n\n" +
                        "`>blackjack new <bet> [ok]` to start a new game\n" +
                        "`>blackjack hit` to draw a card\n" +
                        "`>blackjack stay` to stand")
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
            }

            [Command("new")]
            public async Task BlackjackNewAsync(IContext e)
            {
                var blackjackService = e.GetService<BlackjackService>();

                var userService = e.GetService<IUserService>();     
                var user = await userService.GetOrCreateUserAsync(e.GetAuthor());
                int bet = ValidateBet(e, user);

                var session = await blackjackService.NewSessionAsync(
                        null, e.GetAuthor().Id, e.GetChannel().Id, bet)
                    .AndThen(x => blackjackService.DrawCard(x, BlackjackService.DealerId))
                    .AndThen(x => blackjackService.DrawCard(x, BlackjackService.DealerId))
                    .AndThen(x => blackjackService.DrawCard(x, e.GetAuthor().Id))
                    .AndThen(x => blackjackService.DrawCard(x, e.GetAuthor().Id))
                    .AndThen(x => x.Players[BlackjackService.DealerId].Hand[1].isPublic = false);

                var message = await e.GetChannel().SendMessageAsync(
                    null, embed: CreateEmbed(e, session).ToEmbed());

                session.SetMessageId(message.Id);

                await blackjackService.SyncSessionAsync(session.GetContext());
            }

            [Command("hit", "draw")]
            public async Task OnBlackjackHitAsync(IContext e)
            {
                var blackjackService = e.GetService<BlackjackService>();

                var session = await blackjackService.LoadSessionAsync(
                        e.GetAuthor().Id, e.GetChannel().Id)
                    .ConfigureAwait(false);

                var state = blackjackService.DrawCard(session, e.GetAuthor().Id);
                
                await blackjackService.SyncSessionAsync(session.GetContext());
                await OnStateChangeAsync(e, session, state);
            }

            [Command("stay", "stand")]
            public async Task OnBlackjackHoldAsync(IContext e)
            {
                var blackjackService = e.GetService<BlackjackService>();
                var session = await blackjackService.LoadSessionAsync(
                    e.GetAuthor().Id, e.GetChannel().Id);

                session.Players[BlackjackService.DealerId].ShowAll();

                var state = blackjackService.Stand(session, e.GetAuthor().Id);
                await blackjackService.SyncSessionAsync(session.GetContext());

                await OnStateChangeAsync(e, session, state);
            }

            private Task OnStateChangeAsync(
                IContext ctx, BlackjackSession session, BlackjackState state)
            {
                return state switch
                {
                    BlackjackState.NONE => OnBlackjackNoneAsync(ctx, session),
                    BlackjackState.WIN  => OnBlackjackWinAsync(ctx, session),
                    BlackjackState.LOSE => OnBlackjackDeadAsync(ctx, session),
                    BlackjackState.DRAW => OnBlackjackDrawAsync(ctx, session),
                    _ => throw new InvalidOperationException(),
                };
            }

            private async Task OnBlackjackNoneAsync(
                IContext ctx, BlackjackSession session)
            {
                var apiClient = ctx.GetService<IApiClient>();

                await Retry.RetryAsync(() => apiClient.EditMessageAsync(
                    ctx.GetChannel().Id, 
                    session.MessageId, 
                    new EditMessageArgs
                {
                    Embed = CreateEmbed(ctx, session).ToEmbed()
                }), 1000);
                // TODO: care about the message.
                // TODO: just create a new message instance and allow changing the message id in the
                //       context?
            }

            private async Task OnBlackjackDrawAsync(
                IContext ctx, BlackjackSession session)
            {
                var apiClient = ctx.GetService<IApiClient>();
                var blackjackService = ctx.GetService<BlackjackService>();
                var transactionService = ctx.GetService<ITransactionService>();

                await blackjackService.EndSessionAsync(session);

                await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithSender(AppProps.Currency.BankId)
                        .WithReceiver((long)ctx.GetAuthor().Id)
                        .WithAmount(session.Bet)
                        .Build());

                await Retry.RetryAsync(() => apiClient.EditMessageAsync(
                    ctx.GetChannel().Id,
                    session.MessageId,
                    new EditMessageArgs
                    {
                        Embed = CreateDrawEmbed(ctx, session)
                    }), 5000);
            }

            private async Task OnBlackjackDeadAsync(
                IContext ctx, BlackjackSession session)
            {
                var apiClient = ctx.GetService<IApiClient>();
                var blackjackService = ctx.GetService<BlackjackService>();

                await blackjackService.EndSessionAsync(session);

                try
                {
                    await apiClient.EditMessageAsync(
                        ctx.GetChannel().Id,
                        session.MessageId,
                        new EditMessageArgs
                        {
                            Embed = await CreateLoseEmbedAsync(ctx, session)
                        });
                }
                catch
                {
                    // ignored
                }
            }

            private async Task OnBlackjackWinAsync(
                IContext ctx, BlackjackSession session)
            {
                var apiClient = ctx.GetService<IApiClient>();
                var blackjackService = ctx.GetService<BlackjackService>();
                var transactionService = ctx.GetService<ITransactionService>();

                await blackjackService.EndSessionAsync(session);

                await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithSender(AppProps.Currency.BankId)
                        .WithReceiver((long)ctx.GetAuthor().Id)
                        .WithAmount(session.Bet * 2)
                        .Build());

                try
                {
                    await apiClient.EditMessageAsync(
                        ctx.GetChannel().Id,
                        session.MessageId,
                        new EditMessageArgs
                        {
                            Embed = CreateWinEmbed(ctx, session)
                        });
                }
                catch
                {
                    // ignored
                }
            }

            private async Task<DiscordEmbed> CreateLoseEmbedAsync(
                IContext ctx, BlackjackSession session)
            {
                var locale = ctx.GetLocale();
                var self = await ctx.GetGuild().GetSelfAsync();

                var authorResource =
                    $"{locale.GetString("miki_blackjack_lose_title")} | {ctx.GetAuthor().Username}";

                return CreateEmbed(ctx, session)
                    .SetAuthor(authorResource, self.GetAvatarUrl(), "https://patreon.com/mikibot")
                    .SetDescription(locale.GetString("miki_blackjack_lose_description"))
                    .ToEmbed();
            }

            private DiscordEmbed CreateDrawEmbed(
                IContext ctx, BlackjackSession session)
            {
                var locale = ctx.GetLocale();
                return CreateEmbed(ctx, session)
                    .SetAuthor(
                        locale.GetString("blackjack_draw_title") + " | " + ctx.GetAuthor().Username,
                        ctx.GetAuthor().GetAvatarUrl(),
                        "https://patreon.com/mikibot"
                    )
                    .SetDescription(locale.GetString("blackjack_draw_description"))
                    .ToEmbed();
            }

            private DiscordEmbed CreateWinEmbed(
                IContext ctx, BlackjackSession session)
            {
                var locale = ctx.GetLocale();

                var authorResource =
                    $"{locale.GetString("miki_blackjack_win_title")} | {ctx.GetAuthor().Username}";

                return CreateEmbed(ctx, session)
                    .SetAuthor(
                        authorResource, ctx.GetAuthor().GetAvatarUrl(), "https://patreon.com/mikibot")
                    .SetDescription(locale.GetString("miki_blackjack_win_description", session.Bet * 2))
                    .ToEmbed();
            }

            public EmbedBuilder CreateEmbed(IContext e, BlackjackSession session)
            {
                var locale = e.GetLocale();
                string explanation = locale.GetString("miki_blackjack_explanation");

                CardHand player = session.Players[e.GetAuthor().Id];
                CardHand dealer = session.Players[BlackjackService.DealerId];

                var userHandResource =
                    locale.GetString("miki_blackjack_cards_you", session.GetHandWorth(player));
                var dealerHandResource =
                    locale.GetString("miki_blackjack_cards_miki", session.GetHandWorth(dealer));

                return new EmbedBuilder()
                    .SetAuthor(
                        locale.GetString("miki_blackjack") + " | " + e.GetAuthor().Username,
                        e.GetAuthor().GetAvatarUrl(),
                        "https://patreon.com/mikibot")
                    .SetDescription(
                        $"{explanation}\n"
                        + $"{locale.GetString("miki_blackjack_hit", ">blackjack hit".AsCode())}\n"
                        + locale.GetString("miki_blackjack_stay", ">blackjack stay".AsCode()))
                    .AddField(userHandResource, player.Print(), true)
                    .AddField(dealerHandResource, dealer.Print(), true);
            }
        }

        [Command("flip")]
        public async Task FlipAsync(IContext e)
        {
            var locale = e.GetLocale();
            var transactionService = e.GetService<ITransactionService>();
            var userService = e.GetService<IUserService>();

            var user = await userService.GetOrCreateUserAsync(e.GetAuthor()).ConfigureAwait(false);

            int bet = ValidateBet(e, user, 10000);

            if (e.GetArgumentPack().Pack.Length < 2)
            {
                await e.ErrorEmbed("Please pick either `heads` or `tails`!")
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
                return;
            }

            string sideParam = e.GetArgumentPack().TakeRequired<string>().ToLowerInvariant();

            int? pickedSide = null;
            if (sideParam[0] == 'h')
            {
                pickedSide = 1;
            }
            else if (sideParam[0] == 't')
            {
                pickedSide = 0;
            }

            if (!pickedSide.HasValue)
            {
                await e.ErrorEmbed("This is not a valid option!")
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
                return;
            }

            await transactionService.CreateTransactionAsync(
                new TransactionRequest.Builder()
                    .WithReceiver(AppProps.Currency.BankId)
                    .WithSender((long)e.GetAuthor().Id)
                    .WithAmount(bet)
                    .Build());
            
            string headsUrl = "https://cdn.miki.ai/commands/miki-default-heads.png";
            string tailsUrl = "https://cdn.miki.ai/commands/miki-default-tails.png";

            if (e.GetArgumentPack().Peek(out string bonus))
            {
                if (bonus == "-bonus")
                {
                    headsUrl = "https://cdn.miki.ai/commands/miki-secret-heads.png";
                    tailsUrl = "https://cdn.miki.ai/commands/miki-secret-tails.png";
                }
            }

            int side = MikiRandom.Next(2);
            string imageUrl = side == 1 ? headsUrl : tailsUrl;

            bool win = side == pickedSide;

            if (win)
            {
                await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithReceiver((long)e.GetAuthor().Id)
                        .WithSender(AppProps.Currency.BankId)
                        .WithAmount(bet * 2)
                        .Build());
            }

            string output = win 
                ? locale.GetString("flip_description_win", bet.AsCode())
                : locale.GetString("flip_description_lose");

            output += "\n" + locale.GetString(
                          "miki_blackjack_new_balance",
                          user.Currency + (win ? bet : -bet));

            DiscordEmbed embed = new EmbedBuilder()
                .SetAuthor(
                    $"{locale.GetString("flip_header")} | {e.GetAuthor().Username}", 
                    e.GetAuthor().GetAvatarUrl(),
                    "https://patreon.com/mikibot")
                .SetDescription(output)
                .SetThumbnail(imageUrl)
                .ToEmbed();

            await embed.QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }

        [Command("slots", "s")]
        public async Task SlotsAsync(IContext e)
        {
            var transactionService = e.GetService<ITransactionService>();
            var userService = e.GetService<IUserService>();

            User u = await userService.GetOrCreateUserAsync(e.GetAuthor())
                .ConfigureAwait(false);
            int bet = ValidateBet(e, u, 100000);
            int moneyReturned = 0;

            await transactionService.CreateTransactionAsync(
                new TransactionRequest.Builder()
                    .WithAmount(bet)
                    .WithReceiver(AppProps.Currency.BankId)
                    .WithSender((long)e.GetAuthor().Id)
                    .Build());

            string[] objects = {
                "🍒", "🍒", "🍒", "🍒", "🍒", "🍒", "🍒",
                "🍊", "🍊", "🍊", "🍊", "🍊", "🍊",
                "🍓", "🍓", "🍓", "🍓", "🍓",
                "🍍", "🍍", "🍍", "🍍",
                "🍇", "🍇", "🍇",
                "🍉", "🍉",
                "⭐",
            };

            var locale = e.GetLocale();

            EmbedBuilder embed = new EmbedBuilder()
                .SetAuthor(
                    $"{locale.GetString("miki_module_fun_slots_header")} | {e.GetAuthor().Username}",
                    e.GetAuthor().GetAvatarUrl(), 
                    "https://patreon.com/mikibot");

            string[] objectsChosen = {
                objects[MikiRandom.Next(objects.Length)],
                objects[MikiRandom.Next(objects.Length)],
                objects[MikiRandom.Next(objects.Length)]
            };

            Dictionary<string, int> score = new Dictionary<string, int>();

            foreach (string o in objectsChosen)
            {
                if (score.ContainsKey(o))
                {
                    score[o]++;
                    continue;
                }
                score.Add(o, 1);
            }

            if (score.ContainsKey("🍒"))
            {
                if (score["🍒"] == 2)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 0.25f);
                }
                else if (score["🍒"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 3f);
                }
            }
            if (score.ContainsKey("🍊"))
            {
                if (score["🍊"] == 2)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 0.5f);
                }
                else if (score["🍊"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 5f);
                }
            }
            if (score.ContainsKey("🍓"))
            {
                if (score["🍓"] == 2)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 0.75f);
                }
                else if (score["🍓"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 7f);
                }
            }
            if (score.ContainsKey("🍍"))
            {
                if (score["🍍"] == 2)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 1f);
                }
                if (score["🍍"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 10f);
                }
            }
            if (score.ContainsKey("🍇"))
            {
                if (score["🍇"] == 2)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 2f);
                }
                if (score["🍇"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 15f);
                }
            }
            if (score.ContainsKey("🍉"))
            {
                if (score["🍉"] == 2)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 3f);
                }
                if (score["🍉"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 25f);
                }
            }
            if (score.ContainsKey("⭐"))
            {
                if (score["⭐"] == 2)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 7f);
                }
                if (score["⭐"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet * 75f);

                    var achievements = e.GetService<AchievementService>();
                    var slotsAchievement = achievements.GetAchievement(AchievementIds.SlotsId);
                    await achievements.UnlockAsync(e, slotsAchievement, e.GetAuthor().Id);
                }
            }

            if (moneyReturned == 0)
            {
                embed.AddField(
                    locale.GetString("miki_module_fun_slots_lose_header"),
                    locale.GetString("miki_module_fun_slots_lose_amount", bet, u.Currency - bet));
            }
            else
            {
                embed.AddField(
                    locale.GetString("miki_module_fun_slots_win_header"),
                    locale.GetString(
                        "miki_module_fun_slots_win_amount", 
                        moneyReturned, 
                        u.Currency + moneyReturned));

                await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithAmount(bet + moneyReturned)
                        .WithSender(AppProps.Currency.BankId)
                        .WithReceiver((long)e.GetAuthor().Id)
                        .Build());
            }

            embed.Description = string.Join(" ", objectsChosen);

            await embed.ToEmbed()
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }

        [Command("lottery")]
        public class LotteryCommand
        {
            [Command]
            public async Task LotteryAsync(IContext e)
            {
                var locale = e.GetLocale();
                var lotteryService = e.GetService<ILotteryService>();
                var entry = (await lotteryService.GetEntriesForUserAsync((long)e.GetAuthor().Id))
                    .OrElse(new LotteryEntry { UserId = (long)e.GetAuthor().Id })
                    .Unwrap();
                var jackpot = await lotteryService.GetTotalPrizeAsync();
                var task = await lotteryService.GetLotteryTaskAsync();

                await new EmbedBuilder()
                    .SetTitle($"🍀 {locale.GetString("lottery")}")
                    .SetDescription(locale.GetString("lottery_description"))
                    .SetColor(119, 178, 85)
                    .AddInlineField(
                        locale.GetString("lottery_tickets_owned"), 
                        entry.TicketCount.ToString()) 
                    .AddInlineField(
                        locale.GetString("lottery_draw_time"), 
                        task.GetTimeRemaining().ToTimeString(locale))
                    .AddInlineField(
                        locale.GetString("lottery_ticket_price"),
                        $"{AppProps.Emoji.Mekos} {lotteryService.EntryPrice}")
                    .AddInlineField(
                        locale.GetString("lottery_jackpot"),
                        jackpot.ToString("N0"))
                    .AddInlineField(
                        locale.GetString("lottery_how_to"), 
                        ">lottery buy <amount>")
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
            }

            [Command("buy")]
            public async Task BuyLotteryEntriesAsync(IContext e)
            {
                var amount = e.GetArgumentPack().TakeRequired<int>();
                var lotteryService = e.GetService<ILotteryService>();
                await lotteryService.PurchaseEntriesAsync((long)e.GetAuthor().Id, amount);
                await e.SuccessEmbedResource("lottery_buy_success", amount.ToString("N0"))
                    .QueueAsync(e, e.GetChannel());
            }
        }

        private static int ValidateBet(
            [NotNull] IContext e, 
            [NotNull] User user,
            int maxBet = 1000000)
        {
            var args = e.GetArgumentPack();
            if (args.Take(out int bet)) {}
            else if(args.Take(out string arg))
            {
                if(Utils.IsAll(arg))
                {
                    bet = Math.Min(user.Currency, maxBet);
                }
            }

            if (bet > maxBet)
            {
                throw new BetLimitOverflowException(maxBet);
            }
            return bet;
        }
    }
}
 