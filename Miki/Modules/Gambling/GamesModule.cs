using Miki.Accounts.Achievements;
using Miki.Accounts.Achievements.Objects;
using Miki.API.Cards.Objects;
using Miki.Bot.Models;
using Miki.Bot.Models.Exceptions;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Helpers;
using Miki.Localization;
using Miki.Models;
using Miki.Modules.Gambling.Managers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module("Gambling")]
    public class GamblingModule
    {
        [Command("rps")]
        public async Task RPSAsync(IContext e)
        {
            int? bet = await ValidateBetAsync(e, 10000);
            if (!bet.HasValue)
            {
                return;
            }

            float rewardMultiplier = 1f;

            if (e.GetArgumentPack().Pack.Length < 2)
            {
                await e.ErrorEmbed("You need to choose a weapon!")
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
            }

            User user;
            RPSManager rps = RPSManager.Instance;
            EmbedBuilder resultMessage = new EmbedBuilder()
                .SetTitle("Rock, Paper, Scissors!");

            e.GetArgumentPack().Take(out string weapon);
            var context = e.GetService<MikiDbContext>();

            if (!rps.TryParse(weapon, out RPSWeapon playerWeapon))
            {
                await resultMessage.SetDescription("Invalid weapon!").ToEmbed()
                    .QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                return;
            }

            RPSWeapon botWeapon = rps.GetRandomWeapon();

            resultMessage.SetDescription($"{playerWeapon.Name.ToUpper()} {playerWeapon.Emoji} vs. {botWeapon.Emoji} {botWeapon.Name.ToUpper()}");

            switch (rps.CalculateVictory(playerWeapon, botWeapon))
            {
                case RPSManager.VictoryStatus.WIN:
                {
                    user = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());
                    if (user != null)
                    {
                        await user.AddCurrencyAsync((int)(bet * rewardMultiplier), e.GetChannel());
                        await context.SaveChangesAsync();
                    }
                    resultMessage.Description += $"\n\nYou won `{(int)(bet * rewardMultiplier)}` mekos! Your new balance is `{user.Currency}`.";
                }
                break;

                case RPSManager.VictoryStatus.LOSE:
                {
                    user = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());
                    if (user != null)
                    {
                        user.RemoveCurrency(bet.Value);
                        await context.SaveChangesAsync();
                    }

                    resultMessage.Description += $"\n\nYou lost `{bet}` mekos ! Your new balance is `{user.Currency}`.";
                }
                break;

                case RPSManager.VictoryStatus.DRAW:
                {
                    resultMessage.Description += $"\n\nIt's a draw! no mekos were lost!.";
                }
                break;
            }

            await resultMessage.ToEmbed()
                .QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
        }

        [Command("blackjack", "bj")]
        class BlackjackCommand
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
                    .QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
            }

            [Command("new")]
            public async Task BlackjackNewAsync(IContext e)
            {
                var cache = e.GetService<ICacheClient>();
                int? bet = await ValidateBetAsync(e);

                if (bet.HasValue)
                {
                    if (await cache.ExistsAsync($"miki:blackjack:{e.GetChannel().Id}:{e.GetAuthor().Id}"))
                    {
                        await e.ErrorEmbedResource(new LanguageResource("blackjack_session_exists"))
                            .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                        return;
                    }

                    var context = e.GetService<MikiDbContext>();
                    {
                        var user = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());
                        if (user == null)
                        {
                            return;
                        }

                        user.RemoveCurrency(bet.Value);
                        await context.SaveChangesAsync();
                    }

                    BlackjackManager manager = new BlackjackManager(bet.Value);

                    CardHand dealer = manager.AddPlayer(0);
                    _ = manager.AddPlayer(e.GetAuthor().Id);

                    manager.DealAll();
                    manager.DealAll();

                    dealer.Hand[1].isPublic = false;

                    IDiscordMessage message = await manager.CreateEmbed(e)
                        .ToEmbed().SendToChannel(e.GetChannel() as IDiscordTextChannel);

                    manager.MessageId = message.Id;

                    await cache.UpsertAsync(
                        $"miki:blackjack:{e.GetChannel().Id}:{e.GetAuthor().Id}", 
                        manager.ToContext(), 
                        TimeSpan.FromHours(24));
                }
            }

            [Command("hit", "draw")]
            private async Task OnBlackjackHitAsync(IContext e)
            {
                var cache = e.GetService<ICacheClient>();
                var api = e.GetService<IApiClient>();

                BlackjackManager bm = await BlackjackManager.FromCacheClientAsync(cache, e.GetChannel().Id, e.GetAuthor().Id);

                CardHand Player = bm.GetPlayer(e.GetAuthor().Id);
                CardHand Dealer = bm.GetPlayer(0);

                bm.DealTo(Player);

                if (bm.Worth(Player) > 21)
                {
                    await OnBlackjackDead(e, bm);
                }
                else
                {
                    if (Player.Hand.Count == 5)
                    {
                        await OnBlackjackHold(e, true);
                        return;
                    }
                    else if (bm.Worth(Player) == 21 && bm.Worth(Dealer) != 21)
                    {
                        await OnBlackjackWin(e, bm);
                        return;
                    }
                    else if (bm.Worth(Dealer) == 21 && bm.Worth(Player) != 21)
                    {
                        await OnBlackjackDead(e, bm);
                        return;
                    }

                    await api.EditMessageAsync(e.GetChannel().Id, bm.MessageId, new EditMessageArgs
                    {
                        embed = bm.CreateEmbed(e).ToEmbed()
                    });

                    await cache.UpsertAsync($"miki:blackjack:{e.GetChannel().Id}:{e.GetAuthor().Id}", bm.ToContext(), TimeSpan.FromHours(24));
                }
            }

            [Command("stay", "stand")]
            private async Task OnBlackjackHoldAsync(IContext e)
            {
                var cache = e.GetService<ICacheClient>();
                BlackjackManager bm = await BlackjackManager.FromCacheClientAsync(
                    cache, 
                    e.GetChannel().Id, 
                    e.GetAuthor().Id);

                CardHand Player = bm.GetPlayer(e.GetAuthor().Id);
                CardHand Dealer = bm.GetPlayer(0);

                var charlie = Player.Hand.Count >= 5;

                Dealer.Hand.ForEach(x => x.isPublic = true);

                while (true)
                {
                    if (bm.Worth(Dealer) >= Math.Max(bm.Worth(Player), 17))
                    {
                        if (charlie)
                        {
                            if (Dealer.Hand.Count == 5)
                            {
                                if (bm.Worth(Dealer) == bm.Worth(Player))
                                {
                                    await OnBlackjackDraw(e, bm);
                                    return;
                                }
                                await OnBlackjackDead(e, bm);
                                return;
                            }
                        }
                        else
                        {
                            if (bm.Worth(Dealer) == bm.Worth(Player))
                            {
                                await OnBlackjackDraw(e, bm);
                                return;
                            }
                            await OnBlackjackDead(e, bm);
                            return;
                        }
                    }

                    bm.DealTo(Dealer);

                    if (bm.Worth(Dealer) > 21)
                    {
                        await OnBlackjackWin(e, bm);
                        return;
                    }
                }
            }

            private async Task OnBlackjackDraw(IContext e, BlackjackManager bm)
            {
                var cache = e.GetService<ICacheClient>();
                var api = e.GetService<IApiClient>();

                User user;
                var context = e.GetService<MikiDbContext>();

                user = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());
                if (user != null)
                {
                    await user.AddCurrencyAsync(bm.Bet, e.GetChannel());
                    await context.SaveChangesAsync();
                }

                await api.EditMessageAsync(e.GetChannel().Id, bm.MessageId,
                    new EditMessageArgs
                    {
                        embed = bm.CreateEmbed(e)
                       .SetAuthor(
                            e.GetLocale().GetString("blackjack_draw_title") + " | " + e.GetAuthor().Username,
                            e.GetAuthor().GetAvatarUrl(),
                            "https://patreon.com/mikibot"
                        )
                       .SetDescription(
                            e.GetLocale().GetString("blackjack_draw_description") + "\n" +
                            e.GetLocale().GetString("miki_blackjack_current_balance", user.Currency)
                        ).ToEmbed()
                    }
                );

                await cache.RemoveAsync($"miki:blackjack:{e.GetChannel().Id}:{e.GetAuthor().Id}");
            }

            private async Task OnBlackjackDead(IContext e, BlackjackManager bm)
            {
                var cache = e.GetService<ICacheClient>();
                var api = e.GetService<IApiClient>();

                User user;
                var context = e.GetService<MikiDbContext>();
                user = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());

                await cache.RemoveAsync($"miki:blackjack:{e.GetChannel().Id}:{e.GetAuthor().Id}");

                await api.EditMessageAsync(e.GetChannel().Id, bm.MessageId,
                    new EditMessageArgs
                    {
                        embed = bm.CreateEmbed(e)
                                .SetAuthor(
                                    e.GetLocale().GetString("miki_blackjack_lose_title") + " | " + e.GetAuthor().Username,
                                    (await e.GetGuild().GetSelfAsync()).GetAvatarUrl(), "https://patreon.com/mikibot"
                                )
                                .SetDescription(e.GetLocale().GetString("miki_blackjack_lose_description") + "\n" + e.GetLocale().GetString("miki_blackjack_new_balance", user.Currency)
                                ).ToEmbed()
                    });

            }

            private async Task OnBlackjackWin(IContext e, BlackjackManager bm)
            {
                var cache = e.GetService<ICacheClient>();
                var api = e.GetService<IApiClient>();

                await cache.RemoveAsync($"miki:blackjack:{e.GetChannel().Id}:{e.GetAuthor().Id}");

                User user;
                var context = e.GetService<MikiDbContext>();

                user = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());

                if (user != null)
                {
                    await user.AddCurrencyAsync(bm.Bet * 2, e.GetChannel());

                    await api.EditMessageAsync(e.GetChannel().Id, bm.MessageId, new EditMessageArgs
                    {
                        embed = bm.CreateEmbed(e)
                        .SetAuthor(e.GetLocale().GetString("miki_blackjack_win_title") + " | " + e.GetAuthor().Username, e.GetAuthor().GetAvatarUrl(), "https://patreon.com/mikibot")
                        .SetDescription(e.GetLocale().GetString("miki_blackjack_win_description", bm.Bet * 2) + "\n" + e.GetLocale().GetString("miki_blackjack_new_balance", user.Currency))
                        .ToEmbed()
                    });

                    await context.SaveChangesAsync();
                }
            }
        }

        [Command("flip")]
        public async Task FlipAsync(IContext e)
        {
            int? bet = await ValidateBetAsync(e, 10000);
            if (!bet.HasValue)
            {
                return;
            }

            if (e.GetArgumentPack().Pack.Length < 2)
            {
                await e.ErrorEmbed("Please pick either `heads` or `tails`!")
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
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
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
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
            var context = e.GetService<MikiDbContext>();

            User u = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());
            if (!win)
            {
                u.RemoveCurrency(bet.Value);
            }
            else
            {
                await u.AddCurrencyAsync(bet.Value);
            }

            int currencyNow = u.Currency;

            await context.SaveChangesAsync();

            string output;
            if (win)
            {
                output = e.GetLocale().GetString("flip_description_win", $"`{bet}`");
            }
            else
            {
                output = e.GetLocale().GetString("flip_description_lose");
            }

            output += "\n" + e.GetLocale().GetString("miki_blackjack_new_balance", currencyNow);

            DiscordEmbed embed = new EmbedBuilder()
                .SetAuthor(e.GetLocale().GetString("flip_header") + " | " + e.GetAuthor().Username, e.GetAuthor().GetAvatarUrl(),
                    "https://patreon.com/mikibot")
                .SetDescription(output)
                .SetThumbnail(imageUrl)
                .ToEmbed();

            await embed.QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
        }

        [Command("slots", "s")]
        public async Task SlotsAsync(IContext e)
        {
            int? bet = await ValidateBetAsync(e, 99999);
            if (!bet.HasValue)
            {
                return;
            }

            var context = e.GetService<MikiDbContext>();

            User u = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());

            int moneyReturned = 0;

            string[] objects =
            {
                    "🍒", "🍒", "🍒", "🍒", "🍒", "🍒", "🍒",
                    "🍊", "🍊", "🍊", "🍊", "🍊", "🍊",
                    "🍓", "🍓", "🍓", "🍓", "🍓",
                    "🍍", "🍍", "🍍", "🍍",
                    "🍇", "🍇", "🍇",
                    "🍉", "🍉",
                    "⭐",
                };

            EmbedBuilder embed = new EmbedBuilder()
                .SetAuthor(e.GetLocale().GetString("miki_module_fun_slots_header") + " | " + e.GetAuthor().Username, e.GetAuthor().GetAvatarUrl(), "https://patreon.com/mikibot");

            string[] objectsChosen =
            {
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
                    moneyReturned = (int)Math.Ceiling(bet.Value * 0.25f);
                }
                else if (score["🍒"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet.Value * 3f);
                }
            }
            if (score.ContainsKey("🍊"))
            {
                if (score["🍊"] == 2)
                {
                    moneyReturned = (int)Math.Ceiling(bet.Value * 0.5f);
                }
                else if (score["🍊"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet.Value * 5f);
                }
            }
            if (score.ContainsKey("🍓"))
            {
                if (score["🍓"] == 2)
                {
                    moneyReturned = (int)Math.Ceiling(bet.Value * 0.75f);
                }
                else if (score["🍓"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet.Value * 7f);
                }
            }
            if (score.ContainsKey("🍍"))
            {
                if (score["🍍"] == 2)
                {
                    moneyReturned = (int)Math.Ceiling(bet.Value * 1f);
                }
                if (score["🍍"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet.Value * 10f);
                }
            }
            if (score.ContainsKey("🍇"))
            {
                if (score["🍇"] == 2)
                {
                    moneyReturned = (int)Math.Ceiling(bet.Value * 2f);
                }
                if (score["🍇"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet.Value * 15f);
                }
            }
            if (score.ContainsKey("🍉"))
            {
                if (score["🍉"] == 2)
                {
                    moneyReturned = (int)Math.Ceiling(bet.Value * 3f);
                }
                if (score["🍉"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet.Value * 25f);
                }
            }
            if (score.ContainsKey("⭐"))
            {
                if (score["⭐"] == 2)
                {
                    moneyReturned = (int)Math.Ceiling(bet.Value * 7f);
                }
                if (score["⭐"] == 3)
                {
                    moneyReturned = (int)Math.Ceiling(bet.Value * 75f);

                    await AchievementManager.Instance.GetContainerById("slots").CheckAsync(new BasePacket()
                    {
                        discordChannel = e.GetChannel() as IDiscordTextChannel,
                        discordUser = e.GetAuthor()
                    });
                }
            }

            if (moneyReturned == 0)
            {
                embed.AddField(e.GetLocale()
                    .GetString("miki_module_fun_slots_lose_header"),
                    e.GetLocale()
                    .GetString("miki_module_fun_slots_lose_amount", bet, u.Currency - bet));
                u.RemoveCurrency(bet.Value);
            }
            else
            {
                embed.AddField(e.GetLocale()
                    .GetString("miki_module_fun_slots_win_header"),
                    e.GetLocale().GetString(
                        "miki_module_fun_slots_win_amount", 
                        moneyReturned, u.Currency + moneyReturned));
                await u.AddCurrencyAsync(moneyReturned, e.GetChannel());
            }

            embed.Description = string.Join(" ", objectsChosen);
            await context.SaveChangesAsync();

            await embed.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
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
        //		.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
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
        //						.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
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
        //					.QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
        //			}
        //		}
        //		break;
        //	}
        //}

        public static async Task<int?> ValidateBetAsync(IContext e, int maxBet = 1000000)
        {
            var context = e.GetService<MikiDbContext>();

            User user = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());

            if (user == null)
            {
                throw new UserNullException();
            }

            if (e.GetArgumentPack().Take(out int bet))
            {
            }
            else if (e.GetArgumentPack().Take(out string arg))
            {
                if (arg.ToLower() == "all" || arg == "*")
                {
                    bet = Math.Min(user.Currency, maxBet);
                }
            }

            if (bet == 0)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("miki_error_gambling_parse_error"))
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                return null;
            }

            if (bet < 0)
            {
                throw new ArgumentLessThanZeroException();
            }

            if (bet > user.Currency)
            {
                throw new InsufficientCurrencyException(user.Currency, bet);
            }

            if (bet > maxBet)
            {
                await e.ErrorEmbed($"you cannot bet more than {maxBet:n0} mekos!")
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                return null;
            }

            return bet;
        }
    }
}
 