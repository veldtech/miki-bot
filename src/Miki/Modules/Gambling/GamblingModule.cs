namespace Miki.Modules.Gambling
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Framework.Extension;
    using Miki.API.Cards.Objects;
    using Miki.Bot.Models;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Attributes;
    using Miki.Localization;
    using Miki.Localization.Exceptions;
    using Miki.Modules.Accounts.Services;
    using Miki.Modules.Gambling.Exceptions;
    using Miki.Services.Achievements;
    using Miki.Services.Blackjack;
    using Miki.Services.Rps;
    using Miki.Services.Transactions;
    using Miki.Utility;
    using Services;

    [Module("Gambling")]
    public class GamblingModule
    {
        [Command("rps")]
        public class RpsCommand
        {
            [Command]
            public async Task RpsAsync(IContext e)
            {
                var transactionService = e.GetService<ITransactionService>();
                var userService = e.GetService<IUserService>();

                User user = await userService.GetOrCreateUserAsync(e.GetAuthor())
                    .ConfigureAwait(false);

                int bet = ValidateBet(e, user, 10000);
                await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithReceiver(0L)
                        .WithSender((long)e.GetAuthor().Id)
                        .WithAmount(bet)
                        .Build());

                const float rewardMultiplier = 1f;

                if (e.GetArgumentPack().Pack.Length < 2)
                {
                    await e.ErrorEmbed("You need to choose a weapon!")
                        .ToEmbed()
                        .QueueAsync(e, e.GetChannel())
                        .ConfigureAwait(false);
                }

                var rps = e.GetService<RpsService>();
                EmbedBuilder resultMessage = new EmbedBuilder()
                    .SetTitle("Rock, Paper, Scissors!");

                e.GetArgumentPack().Take(out string weapon);

                if (!RpsWeapon.TryParse(weapon, out RpsWeapon playerWeapon))
                {
                    await resultMessage.SetDescription("Invalid weapon!")
                        .ToEmbed()
                        .QueueAsync(e, e.GetChannel())
                        .ConfigureAwait(false);
                    return;
                }

                RpsWeapon botWeapon = rps.GetRandomWeapon();

                resultMessage.SetDescription($"{playerWeapon.Name.ToUpper()} {playerWeapon.Emoji} vs. {botWeapon.Emoji} {botWeapon.Name.ToUpper()}");

                switch (rps.CalculateVictory(playerWeapon, botWeapon))
                {
                    case RpsService.VictoryStatus.WIN:
                    {
                        await transactionService.CreateTransactionAsync(
                            new TransactionRequest.Builder()
                                .WithAmount((int)(bet * rewardMultiplier))
                                .WithReceiver((long)e.GetAuthor().Id)
                                .WithSender(0L)
                                .Build());
                        resultMessage.Description += $"\n\nYou won `{(int) (bet * rewardMultiplier)}` " 
                                                     + $"mekos! Your new balance is `{user.Currency}`.";
                    } break;

                    case RpsService.VictoryStatus.LOSE:
                    {
                        resultMessage.Description +=
                            $"\n\nYou lost `{bet}` mekos! Your new balance is `{user.Currency}`.";
                    } break;

                    case RpsService.VictoryStatus.DRAW:
                    {
                        resultMessage.Description += "\n\nIt's a draw! no mekos were lost!.";
                    } break;
                }
                await resultMessage.ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
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

                var message = await e.GetChannel()
                    .SendMessageAsync(null, embed: NewLoadingEmbed())
                    .ConfigureAwait(false);

                try
                {
                    var session = await blackjackService.NewSessionAsync(
                        message.Id, e.GetAuthor().Id, e.GetChannel().Id, bet)
                        .AndThen(x => blackjackService.DrawCard(x, BlackjackService.DealerId))
                        .AndThen(x => blackjackService.DrawCard(x, BlackjackService.DealerId))
                        .AndThen(x => blackjackService.DrawCard(x, e.GetAuthor().Id))
                        .AndThen(x => blackjackService.DrawCard(x, e.GetAuthor().Id))
                        .AndThen(x => x.Players[BlackjackService.DealerId].Hand[1].isPublic = false)
                        .AndThen(x => blackjackService.SyncSessionAsync(x.GetContext()));

                    await message.EditAsync(new EditMessageArgs(
                        embed: CreateEmbed(e, session).ToEmbed()))
                        .ConfigureAwait(false);
                }
                catch(LocalizedException ex)
                {
                    await message.EditAsync(new EditMessageArgs(
                        embed: e.ErrorEmbedResource(ex.LocaleResource).ToEmbed()))
                        .ConfigureAwait(false);
                }
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
                await OnStateChange(e, session, state);
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

                await OnStateChange(e, session, state);
            }

            private Task OnStateChange(
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

                await apiClient.EditMessageAsync(
                    ctx.GetChannel().Id, 
                    session.MessageId, 
                    new EditMessageArgs
                {
                    Embed = CreateEmbed(ctx, session).ToEmbed()
                });
                // TODO: care about the message.
                // TODO: just create a new message instance and allow changing the message id in the context?
            }

            private async Task OnBlackjackDrawAsync(
                IContext ctx, BlackjackSession session)
            {
                var apiClient = ctx.GetService<IApiClient>();
                var blackjackService = ctx.GetService<BlackjackService>();
                var transactionService = ctx.GetService<TransactionService>();

                await blackjackService.EndSessionAsync(session);

                await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithSender((long)BlackjackService.DealerId)
                        .WithReceiver((long)ctx.GetAuthor().Id)
                        .WithAmount(session.Bet)
                        .Build());

                try
                {
                    await apiClient.EditMessageAsync(
                        ctx.GetChannel().Id,
                        session.MessageId,
                        new EditMessageArgs
                        {
                            Embed = CreateDrawEmbed(ctx, session)
                        });
                }
                catch { }
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
                catch { }
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
                        .WithSender((long)BlackjackService.DealerId)
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
                catch { }
            }

            private DiscordEmbed NewLoadingEmbed()
            {
                // TODO: Move to resources.
                return new EmbedBuilder()
                    .SetTitle("One moment...")
                    .SetDescription("We're setting up your blackjack!")
                    .ToEmbed();
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
                        // TODO: move command identifiers from resources.
                        $"{explanation}\n{locale.GetString("miki_blackjack_hit")}\n{locale.GetString("miki_blackjack_stay")}")
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

            User user = await userService.GetOrCreateUserAsync(e.GetAuthor())
                .ConfigureAwait(false);

            int bet = ValidateBet(e, user, 10000);

            await transactionService.CreateTransactionAsync(
                new TransactionRequest.Builder()
                    .WithReceiver(AppProps.Currency.BankId)
                    .WithSender((long)e.GetAuthor().Id)
                    .WithAmount(bet)
                    .Build());

            if (e.GetArgumentPack().Pack.Length < 2)
            {
                await e.ErrorEmbed("Please pick either `heads` or `tails`!")
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
                return;
            }

            string sideParam = e.GetArgumentPack().TakeRequired<string>()
                .ToLowerInvariant();

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

            string headsUrl = "https://miki-cdn.nyc3.digitaloceanspaces.com/commands/miki-default-heads.png";
            string tailsUrl = "https://miki-cdn.nyc3.digitaloceanspaces.com/commands/miki-default-tails.png";

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
                ? locale.GetString("flip_description_win", $"`{bet}`")
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
            int bet = ValidateBet(e, u, 99999);
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

        //[Command(Name = "lottery")]
        //public async Task LotteryAsync(EventContext e)
        //{
        //	ArgObject arg = e.GetArgumentPack().FirstOrDefault();

        //	if (arg == null)
        //	{
        //		long totalTickets = await (Global.RedisClient as StackExchangeCacheClient).Client.GetDatabase(0).ListLengthAsync(lotteryKey);
        //		long yourTickets = 0;

        //		string latestWinner = (Global.RedisClient as StackExchangeCacheClient).Client.GetDatabase(0).StringGet("lottery:winner");

        //		if (await lotteryDict.ContainsAsync(e.GetAuthor().Id))
        //		{
        //			yourTickets = long.Parse(await lotteryDict.GetAsync(e.GetAuthor().Id));
        //		}

        //		string timeLeft = taskScheduler?.GetInstance(0, lotteryId).TimeLeft.ToTimeString(e.GetLocale(), true) ?? "1h?m?s - will be fixed soon!";

        //		new EmbedBuilder()
        //		{
        //			Title = "🍀 Lottery",
        //			Description = "Make the biggest gamble, and get paid off massively if legit.",
        //			Color = new Color(119, 178, 85)
        //		}.AddInlineField("Tickets Owned", yourTickets.ToString())
        //		.AddInlineField("Drawing In", timeLeft)
        //		.AddInlineField("Total Tickets", totalTickets.ToString())
        //		.AddInlineField("Ticket price", $"{100} mekos")
        //		.AddInlineField("Latest Winner", latestWinner ?? "no name")
        //		.AddInlineField("How to buy?", ">lottery buy [amount]")
        //		.ToEmbed().QueueToChannelAsync(e.GetChannel());
        //		return;
        //	}

        //	switch (arg.Argument.ToLower())
        //	{
        //		case "buy":
        //		{
        //			arg = arg.Next();
        //			int amount = arg?.AsInt() ?? 1;

        //			if (amount < 1)
        //				amount = 1;

        //			using (var context = new MikiContext())
        //			{
        //				User u = await DatabaseHelpers.GetUserAsync(context, e.GetAuthor());

        //				if (amount * 100 > u.Currency)
        //				{
        //					e.ErrorEmbedResource("miki_mekos_insufficient")
        //						.ToEmbed().QueueToChannelAsync(e.GetChannel());
        //					return;
        //				}

        //				await u.AddCurrencyAsync(-amount * 100, e.GetChannel());

        //				RedisValue[] tickets = new RedisValue[amount];

        //				for (int i = 0; i < amount; i++)
        //				{
        //					tickets[i] = e.GetAuthor().Id.ToString();
        //				}

        //				await (Global.RedisClient as StackExchangeCacheClient).Client.GetDatabase(0).ListRightPushAsync(lotteryKey, tickets);

        //				int totalTickets = 0;

        //				if (await lotteryDict.ContainsAsync(e.GetAuthor().Id.ToString()))
        //				{
        //					totalTickets = int.Parse(await lotteryDict.GetAsync(e.GetAuthor().Id.ToString()));
        //				}

        //				await lotteryDict.AddAsync(e.GetAuthor().Id, amount + totalTickets);

        //				await context.SaveChangesAsync();

        //				e.SuccessEmbed($"Successfully bought {amount} tickets!")
        //					.QueueToChannelAsync(e.GetChannel());
        //			}
        //		}
        //		break;
        //	}
        //}

        public static int ValidateBet(
            [NotNull] IContext e, 
            [NotNull] User user,
            int maxBet = 1000000)
        {
            var args = e.GetArgumentPack();
            if (args.Take(out int bet))
            {
            }
            else if (args.Take(out string arg))
            {
                if (IsValidBetAll(arg))
                {
                    bet = Math.Min(user.Currency, maxBet);
                }
            }

            if (bet > maxBet)
            {
                throw new BetLimitOverflowException();
            }

            return bet;
        }

        private static bool IsValidBetAll(string input)
        {
            return input.ToLowerInvariant() == "all"
                || input.ToLowerInvariant() == "*";
        }
    }
}
 