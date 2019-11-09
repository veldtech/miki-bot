
namespace Miki.Modules.Gambling
{
    using Miki.API.Cards.Objects;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Exceptions;
    using Miki.Cache;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Attributes;
    using Miki.Helpers;
    using Miki.Localization;
    using Miki.Modules.Gambling.Exceptions;
    using Miki.Services.Achievements;
    using Miki.Services.Rps;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Framework.Extension;
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
                var context = e.GetService<MikiDbContext>();

                User user = await User.GetAsync(context, e.GetAuthor().Id, e.GetAuthor().Username)
                    .ConfigureAwait(false);
                if (user == null)
                {
                    return;
                }

                int bet = ValidateBet(e, user, 10000);

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

                if (!rps.TryParse(weapon, out RpsWeapon playerWeapon))
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
                        user.AddCurrency((int) (bet * rewardMultiplier));
                        await context.SaveChangesAsync()
                            .ConfigureAwait(false);
                        resultMessage.Description += $"\n\nYou won `{(int) (bet * rewardMultiplier)}` " 
                                                     + $"mekos! Your new balance is `{user.Currency}`.";
                    } break;

                    case RpsService.VictoryStatus.LOSE:
                    {
                        user.RemoveCurrency(bet);
                        await context.SaveChangesAsync()
                            .ConfigureAwait(false);

                        resultMessage.Description +=
                            $"\n\nYou lost `{bet}` mekos ! Your new balance is `{user.Currency}`.";
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

        //[Command("blackjack", "bj")]
        //public class BlackjackCommand
        //{
        //    [Command]
        //    public async Task BlackjackAsync(IContext e)
        //    {            
        //        await new EmbedBuilder()
        //            .SetTitle("🎲 Blackjack")
        //            .SetColor(234, 89, 110)
        //            .SetDescription("Play a game of blackjack against miki!\n\n" +
        //                "`>blackjack new <bet> [ok]` to start a new game\n" +
        //                "`>blackjack hit` to draw a card\n" +
        //                "`>blackjack stay` to stand")
        //            .ToEmbed()
        //            .QueueAsync(e, e.GetChannel())
        //            .ConfigureAwait(false);
        //    }

        //    [Command("new")]
        //    public async Task BlackjackNewAsync(IContext e)
        //    {
        //        var cache = e.GetService<ICacheClient>();
        //        using var userService = e.GetService<IUserService>();

        //        var user = await userService.GetUserAsync(e.GetAuthor().Id.ToDbLong())
        //            .ConfigureAwait(false);
        //        if(user == null)
        //        {
        //            return;
        //        }

        //        int bet = ValidateBet(e, user);

        //        user.RemoveCurrency(bet);

        //        if(await cache.ExistsAsync($"miki:blackjack:{e.GetChannel().Id}:{e.GetAuthor().Id}")
        //            .ConfigureAwait(false))
        //        {
        //            await e.ErrorEmbedResource("blackjack_session_exists")
        //                .ToEmbed()
        //                .QueueAsync(e, e.GetChannel())
        //                .ConfigureAwait(false);
        //            return;
        //        }

        //        BlackjackManager manager = new BlackjackManager(bet);

        //        CardHand dealer = manager.AddPlayer(0);
        //        _ = manager.AddPlayer(e.GetAuthor().Id);

        //        manager.DealAll();
        //        manager.DealAll();

        //        dealer.Hand[1].isPublic = false;

        //        IDiscordMessage message = await manager.CreateEmbed(e)
        //            .ToEmbed()
        //            .SendToChannel(e.GetChannel())
        //            .ConfigureAwait(false);

        //        manager.MessageId = message.Id;

        //        await cache.UpsertAsync(
        //            $"miki:blackjack:{e.GetChannel().Id}:{e.GetAuthor().Id}",
        //            manager.ToContext(),
        //            TimeSpan.FromHours(24))
        //            .ConfigureAwait(false);

        //        await userService.SaveAsync();
        //    }

        //    [Command("hit", "draw")]
        //    public async Task OnBlackjackHitAsync(IContext e)
        //    {
        //        var cache = e.GetService<ICacheClient>();
        //        var api = e.GetService<IApiClient>();

        //        BlackjackManager bm = await BlackjackManager.FromCacheClientAsync(
        //                cache, 
        //                e.GetChannel().Id, 
        //                e.GetAuthor().Id)
        //            .ConfigureAwait(false);

        //        CardHand player = bm.GetPlayer(e.GetAuthor().Id);
        //        CardHand dealer = bm.GetPlayer(0);

        //        bm.DealTo(player);

        //        if(bm.Worth(player) > 21)
        //        {
        //            await OnBlackjackDeadAsync(e, bm)
        //                .ConfigureAwait(false);
        //        }
        //        else
        //        {
        //            if(player.Hand.Count == 5)
        //            {
        //                await OnBlackjackHoldAsync(e)
        //                    .ConfigureAwait(false);
        //                return;
        //            }
        //            else if(bm.Worth(player) == 21 && bm.Worth(dealer) != 21)
        //            {
        //                await OnBlackjackWinAsync(e, bm)
        //                    .ConfigureAwait(false);
        //                return;
        //            }
        //            else if(bm.Worth(dealer) == 21 && bm.Worth(player) != 21)
        //            {
        //                await OnBlackjackDeadAsync(e, bm)
        //                    .ConfigureAwait(false);
        //                return;
        //            }

        //            await api.EditMessageAsync(e.GetChannel().Id, bm.MessageId, new EditMessageArgs
        //            {
        //                Embed = bm.CreateEmbed(e).ToEmbed()
        //            }).ConfigureAwait(false);

        //            await cache.UpsertAsync(
        //                    $"miki:blackjack:{e.GetChannel().Id}:{e.GetAuthor().Id}", 
        //                    bm.ToContext(), 
        //                    TimeSpan.FromHours(24))
        //                .ConfigureAwait(false);
        //        }
        //    }

        //    [Command("stay", "stand")]
        //    public async Task OnBlackjackHoldAsync(IContext e)
        //    {
        //        var cache = e.GetService<ICacheClient>();
        //        BlackjackManager bm = await BlackjackManager.FromCacheClientAsync(
        //                cache,
        //                e.GetChannel().Id,
        //                e.GetAuthor().Id)
        //            .ConfigureAwait(false);

        //        CardHand player = bm.GetPlayer(e.GetAuthor().Id);
        //        CardHand dealer = bm.GetPlayer(0);

        //        var charlie = player.Hand.Count >= 5;

        //        dealer.Hand.ForEach(x => x.isPublic = true);

        //        while (true)
        //        {
        //            if(bm.Worth(dealer) >= Math.Max(bm.Worth(player), 17))
        //            {
        //                if (charlie)
        //                {
        //                    if(dealer.Hand.Count == 5)
        //                    {
        //                        if(bm.Worth(dealer) == bm.Worth(player))
        //                        {
        //                            await OnBlackjackDrawAsync(e, bm)
        //                                .ConfigureAwait(false);
        //                            return;
        //                        }
        //                        await OnBlackjackDeadAsync(e, bm)
        //                            .ConfigureAwait(false);
        //                        return;
        //                    }
        //                }
        //                else
        //                {
        //                    if(bm.Worth(dealer) == bm.Worth(player))
        //                    {
        //                        await OnBlackjackDrawAsync(e, bm)
        //                            .ConfigureAwait(false);
        //                        return;
        //                    }
        //                    await OnBlackjackDeadAsync(e, bm)
        //                        .ConfigureAwait(false);
        //                    return;
        //                }
        //            }

        //            bm.DealTo(dealer);

        //            if(bm.Worth(dealer) > 21)
        //            {
        //                await OnBlackjackWinAsync(e, bm)
        //                    .ConfigureAwait(false);
        //                return;
        //            }
        //        }
        //    }

        //    private async Task OnBlackjackDrawAsync(IContext e, BlackjackManager bm)
        //    {
        //        var cache = e.GetService<ICacheClient>();
        //        var api = e.GetService<IApiClient>();

        //        var context = e.GetService<MikiDbContext>();

        //        User user = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());
        //        if(user != null)
        //        {
        //            user.AddCurrency(bm.Bet);
        //            await context.SaveChangesAsync()
        //                .ConfigureAwait(false);
        //        }

        //        await api.EditMessageAsync(e.GetChannel().Id, bm.MessageId,
        //            new EditMessageArgs
        //            {
        //                Embed = bm.CreateEmbed(e)
        //               .SetAuthor(
        //                    e.GetLocale().GetString("blackjack_draw_title") + " | " + e.GetAuthor().Username,
        //                    e.GetAuthor().GetAvatarUrl(),
        //                    "https://patreon.com/mikibot"
        //                )
        //               .SetDescription(
        //                    e.GetLocale().GetString("blackjack_draw_description") + "\n" +
        //                    e.GetLocale().GetString("miki_blackjack_current_balance", user.Currency)
        //                ).ToEmbed()
        //            }).ConfigureAwait(false);

        //        await cache.RemoveAsync($"miki:blackjack:{e.GetChannel().Id}:{e.GetAuthor().Id}")
        //            .ConfigureAwait(false);
        //    }

        //    private async Task OnBlackjackDeadAsync(IContext e, BlackjackManager bm)
        //    {
        //        var cache = e.GetService<ICacheClient>();
        //        var api = e.GetService<IApiClient>();

        //        var locale = e.GetLocale();

        //        var context = e.GetService<MikiDbContext>();
        //        User user = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong())
        //            .ConfigureAwait(false);

        //        await cache.RemoveAsync($"miki:blackjack:{e.GetChannel().Id}:{e.GetAuthor().Id}")
        //            .ConfigureAwait(false);

        //        await api.EditMessageAsync(e.GetChannel().Id, bm.MessageId,
        //            new EditMessageArgs
        //            {
        //                Embed = bm.CreateEmbed(e)
        //                        .SetAuthor(
        //                            e.GetLocale().GetString("miki_blackjack_lose_title") + 
        //                            " | " + e.GetAuthor().Username,
        //                            (await e.GetGuild().GetSelfAsync()).GetAvatarUrl(), 
        //                            "https://patreon.com/mikibot")
        //                        .SetDescription(
        //                            locale.GetString("miki_blackjack_lose_description") + "\n" 
        //                            + locale.GetString("miki_blackjack_new_balance", user.Currency))
        //                        .ToEmbed()
        //            }).ConfigureAwait(false);
        //    }

        //    private async Task OnBlackjackWinAsync(IContext e, BlackjackManager bm)
        //    {
        //        var cache = e.GetService<ICacheClient>();
        //        var api = e.GetService<IApiClient>();

        //        await cache.RemoveAsync($"miki:blackjack:{e.GetChannel().Id}:{e.GetAuthor().Id}")
        //            .ConfigureAwait(false);

        //        var context = e.GetService<MikiDbContext>();

        //        User user = await User.GetAsync(context, e.GetAuthor().Id, e.GetAuthor().Username)
        //            .ConfigureAwait(false);
        //        if(user != null)
        //        {
        //            user.AddCurrency(bm.Bet * 2);
        //            await api.EditMessageAsync(e.GetChannel().Id, bm.MessageId, new EditMessageArgs
        //            {
        //                Embed = bm.CreateEmbed(e)
        //                    .SetAuthor(
        //                        e.GetLocale().GetString("miki_blackjack_win_title") 
        //                        + " | " + e.GetAuthor().Username,
        //                        e.GetAuthor().GetAvatarUrl(),
        //                        "https://patreon.com/mikibot")
        //                    .SetDescription(
        //                        e.GetLocale().GetString("miki_blackjack_win_description", bm.Bet * 2)
        //                        + "\n" 
        //                        + e.GetLocale().GetString("miki_blackjack_new_balance", user.Currency))
        //                    .ToEmbed()
        //            }).ConfigureAwait(false);

        //            await context.SaveChangesAsync()
        //                .ConfigureAwait(false);
        //        }
        //    }
        //}

        [Command("flip")]
        public async Task FlipAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            User u = await User.GetAsync(context, e.GetAuthor().Id, e.GetAuthor().Username)
                .ConfigureAwait(false);

            int bet = ValidateBet(e, u, 10000);

            if (e.GetArgumentPack().Pack.Length < 2)
            {
                await e.ErrorEmbed("Please pick either `heads` or `tails`!")
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
                return;
            }

            e.GetArgumentPack().Take(out string sideParam);

            int pickedSide = -1;

            if (char.ToLower(sideParam[0]) == 'h')
            {
                pickedSide = 1;
            }
            else if (char.ToLower(sideParam[0]) == 't')
            {
                pickedSide = 0;
            }

            if (pickedSide == -1)
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

            bool win = (side == pickedSide);
            if (!win)
            {
                u.RemoveCurrency(bet);
            }
            else
            {
                u.AddCurrency(bet);
            }

            await context.SaveChangesAsync()
                .ConfigureAwait(false);

            string output = win ? e.GetLocale().GetString("flip_description_win", $"`{bet}`")
                : e.GetLocale().GetString("flip_description_lose");

            output += "\n" + e.GetLocale().GetString("miki_blackjack_new_balance", u.Currency);

            DiscordEmbed embed = new EmbedBuilder()
                .SetAuthor(e.GetLocale().GetString("flip_header") + " | " + e.GetAuthor().Username, e.GetAuthor().GetAvatarUrl(),
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
            var context = e.GetService<MikiDbContext>();
            if(context == null)
            {
                throw new NotSupportedException();
            }

            User u = await User.GetAsync(context, e.GetAuthor().Id, e.GetAuthor().Username)
                .ConfigureAwait(false);
            int bet = ValidateBet(e, u, 99999);
            int moneyReturned = 0;

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
                    var slotsAchievement = achievements.GetAchievement("slots");
                    await achievements.UnlockAsync(context, slotsAchievement, e.GetAuthor().Id);

                }
            }

            if (moneyReturned == 0)
            {
                embed.AddField(
                    locale.GetString("miki_module_fun_slots_lose_header"),
                    locale.GetString("miki_module_fun_slots_lose_amount", bet, u.Currency - bet));
                u.RemoveCurrency(bet);
            }
            else
            {
                embed.AddField(
                    locale.GetString("miki_module_fun_slots_win_header"),
                    locale.GetString(
                        "miki_module_fun_slots_win_amount", 
                        moneyReturned, 
                        u.Currency + moneyReturned));
                u.AddCurrency(moneyReturned);
            }
            embed.Description = string.Join(" ", objectsChosen);

            await context.SaveChangesAsync()
                .ConfigureAwait(false);
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

        public static int ValidateBet(IContext e, User user, int maxBet = 1000000)
        {
            if(e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            if(user == null)
            {
                throw new UserNullException();
            }

            if (e.GetArgumentPack().Take(out int bet))
            {
            }
            else if (e.GetArgumentPack().Take(out string arg))
            {
                if (IsValidBetAll(arg))
                {
                    bet = Math.Min(user.Currency, maxBet);
                }
            }

            if (bet <= 0)
            {
                throw new ArgumentLessThanZeroException();
            }

            if (bet > user.Currency)
            {
                throw new InsufficientCurrencyException(user.Currency, bet);
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
 