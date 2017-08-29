using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Languages;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Miki.API.Cards;
using Miki.API.Cards.Enums;
using Miki.API.Cards.Objects;
using Miki.Modules.Gambling.Managers;

namespace Miki.Modules
{
    [Module("Gambling")]
    public class GamblingModule
    {
        [Command(Name = "blackjack", Aliases = new[] {"bj"})]
        public async Task BlackjackAsync(EventContext e)
        {
            Locale locale = e.Channel.GetLocale();

            if (Bot.instance.Events.PrivateCommandHandlerExist(e.Author.Id, e.Channel.Id))
            {
                await e.ErrorEmbed(e.GetResource("blackjack_error_instance_exists"))
                    .SendToChannel(e.Channel);

                return;
            }

            await ValidateBet(e, StartBlackjack, 9999999);
        }

        public async Task StartBlackjack(EventContext e, int bet)
        {
            using (var context = new MikiContext())
            {
                User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                await user.RemoveCurrencyAsync(context, null, bet);
            }

            BlackjackManager bm = new BlackjackManager();

            IDiscordMessage message = await bm.CreateEmbed(e).SendToChannel(e.Channel);

            CommandHandler c = new CommandHandlerBuilder(Bot.instance.Events)
                .AddPrefix("")
                .SetOwner(e.message)
                .AddCommand(
                    new RuntimeCommandEvent("hit")
                        .Default(async (ec) => await OnBlackjackHit(ec, bm, message, bet)))
                .AddCommand(
                    new RuntimeCommandEvent("stand")
                        .SetAliases("knock", "stay", "stop")
                        .Default(async (ec) => await OnBlackjackHold(ec, bm, message, bet))
                ).Build();

            Bot.instance.Events.AddPrivateCommandHandler(e.message, c);

        }     

        private async Task OnBlackjackHit(EventContext e, BlackjackManager bm, IDiscordMessage instanceMessage, int bet)
        {
            await e.message.DeleteAsync();

            bm.player.AddToHand(bm.deck.DrawRandom());

            if (bm.Worth(bm.player) > 21)
            {
                await OnBlackjackDead(e, bm, instanceMessage, bet);
            }
            else
            {
                if (bm.player.Hand.Count == 5)
                {
                    await OnBlackjackHold(e, bm, instanceMessage, bet, true);
                    return;
                }
                await bm.CreateEmbed(e).ModifyMessage(instanceMessage);
            }
        }

        private async Task OnBlackjackHold(EventContext e, BlackjackManager bm, IDiscordMessage instanceMessage,
            int bet, bool charlie = false)
        {
            bm.dealer.Hand.ForEach(x => x.isPublic = true);

            if (!charlie)
            {
                await e.message.DeleteAsync();
            }

            while (true)
            {
                if (bm.Worth(bm.dealer) >= bm.Worth(bm.player))
                {
                    if (charlie)
                    {
                        if (bm.dealer.Hand.Count == 5)
                        {
                            if (bm.Worth(bm.dealer) == bm.Worth(bm.player))
                            {
                                await OnBlackjackDraw(e, bm, instanceMessage, bet);
                                return;
                            }
                            await OnBlackjackDead(e, bm, instanceMessage, bet);
                            return;
                        }
                    }
                    else
                    {
                        if (bm.Worth(bm.dealer) == bm.Worth(bm.player))
                        {
                            await OnBlackjackDraw(e, bm, instanceMessage, bet);
                            return;
                        }
                        await OnBlackjackDead(e, bm, instanceMessage, bet);
                        return;
                    }
                }

                bm.dealer.AddToHand(bm.deck.DrawRandom());

                if (bm.Worth(bm.dealer) > 21)
                {
                    await OnBlackjackWin(e, bm, instanceMessage, bet);
                    return;
                }
            }
        }

        private async Task OnBlackjackDraw(EventContext e, BlackjackManager bm, IDiscordMessage instanceMessage,
            int bet)
        {
            await e.commandHandler.RequestDisposeAsync();

            User user;
            using (var context = new MikiContext())
            {
                user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                if (user != null)
                {
                    await user.AddCurrencyAsync(bet, e.Channel);
                    await context.SaveChangesAsync();
                }
            }

            await bm.CreateEmbed(e)
                .SetAuthor(e.GetResource("miki_blackjack_draw_title") + " | " + e.Author.Username, e.Author.AvatarUrl, "https://patreon.com/mikibot")
                .SetDescription(e.GetResource("blackjack_draw_description") + "\n" +
                                e.GetResource("miki_blackjack_current_balance", user.Currency))
                .ModifyMessage(instanceMessage);
        }

        private async Task OnBlackjackDead(EventContext e, BlackjackManager bm, IDiscordMessage instanceMessage,
            int bet)
        {
            await e.commandHandler.RequestDisposeAsync();

            User user;
            using (var context = new MikiContext())
            {
                user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
            }

            await bm.CreateEmbed(e)
                .SetAuthor(e.GetResource("miki_blackjack_lose_title") + " | " + e.Author.Username, e.message.Bot.AvatarUrl, "https://patreon.com/mikibot")
                .SetDescription(e.GetResource("miki_blackjack_lose_description") + "\n" +
                                e.GetResource("miki_blackjack_new_balance", user.Currency))
                .ModifyMessage(instanceMessage);
        }

        private async Task OnBlackjackWin(EventContext e, BlackjackManager bm, IDiscordMessage instanceMessage, int bet)
        {
            await e.commandHandler.RequestDisposeAsync();

            User user;
            using (var context = new MikiContext())
            {
                user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                if (user != null)
                {
                    await user.AddCurrencyAsync(bet * 2, e.Channel);
                    await context.SaveChangesAsync();
                }
            }

            await bm.CreateEmbed(e)
                .SetAuthor(e.GetResource("miki_blackjack_win_title") + " | " + e.Author.Username, e.Author.AvatarUrl, "https://patreon.com/mikibot")
                .SetDescription(e.GetResource("miki_blackjack_win_description", bet * 2) + "\n" +
                                e.GetResource("miki_blackjack_new_balance", user.Currency))
                .ModifyMessage(instanceMessage);
        }

        [Command(Name = "flip")]
        public async Task FlipAsync(EventContext e)
        {
            await ValidateBet(e, StartFlip, 9999);
        }

        private async Task StartFlip(EventContext e, int bet)
        {
            string[] arguments = e.arguments.Split(' ');

            if (bet <= 0)
            {
                return;
            }

            if (arguments.Length < 2)
            {
                return;
            }

            int pickedSide = -1;

            if (arguments[1].ToLower() == "heads" || arguments[1].ToLower() == "h") pickedSide = 1;
            else if (arguments[1].ToLower() == "tails" || arguments[1].ToLower() == "t") pickedSide = 0;

            if (pickedSide == -1)
            {
                return;
            }

            string headsUrl = "https://miki.ai/assets/img/miki-default-heads.png";
            string tailsUrl = "https://miki.ai/assets/img/miki-default-tails.png";

            if (e.arguments.Contains("-bonus"))
            {
                headsUrl = "https://miki.ai/assets/img/miki-secret-heads.png";
                tailsUrl = "https://miki.ai/assets/img/miki-secret-tails.png";
            }

            int side = MikiRandom.Next(2);
            string imageUrl = side == 1 ? headsUrl : tailsUrl;

            bool win = (side == pickedSide);
            int currencyNow = 0;

            using (MikiContext context = new MikiContext())
            {
                User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                if (!win) bet = -bet;
                u.Currency += bet;
                currencyNow = u.Currency;
                await context.SaveChangesAsync();
            }

            string output = "";

            if(win)
            {
                output = e.GetResource("flip_description_win", $"`{bet}`");
            }
            else
            {
                output = e.GetResource("flip_description_lose");
            }

            output += "\n" + e.GetResource("miki_blackjack_new_balance", currencyNow);

            IDiscordEmbed embed = Utils.Embed
                .SetAuthor(e.GetResource("flip_header") + " | " + e.Author.Username, e.Author.AvatarUrl,
                    "https://patreon.com/mikibot")
                .SetDescription(output)
                .SetImageUrl(imageUrl);

            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "slots", Aliases = new[] {"s"})]
        public async Task SlotsAsync(EventContext e)
        {
            await ValidateBet(e, StartSlots, 99999);
        }

        public async Task StartSlots(EventContext e, int bet)
        {
            using (var context = new MikiContext())
            {
                User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                int moneyReturned = 0;

                if (bet <= 0)
                {
                    return;
                }

                string[] objects =
                {
                    "🍒", "🍒", "🍒", "🍒",
                    "🍊", "🍊", "🍊",
                    "🍓", "🍓",
                    "🍍", "🍍",
                    "🍇", "🍇",
                    "🍉", "🍉",
                    "⭐", "⭐",
                    "🍉",
                    "🍍", "🍍",
                    "🍓", "🍓",
                    "🍊", "🍊", "🍊",
                    "🍒", "🍒", "🍒", "🍒",
                };

                IDiscordEmbed embed = Utils.Embed
                    .SetAuthor(locale.GetString(Locale.SlotsHeader) + " | " + e.Author.Username, e.Author.AvatarUrl, "https://patreon.com/mikibot");

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
                        moneyReturned = (int) Math.Ceiling(bet * 0.5f);
                    }
                    else if (score["🍒"] == 3)
                    {
                        moneyReturned = (int) Math.Ceiling(bet * 1f);
                    }
                }
                if (score.ContainsKey("🍊"))
                {
                    if (score["🍊"] == 2)
                    {
                        moneyReturned = (int) Math.Ceiling(bet * 0.8f);
                    }
                    else if (score["🍊"] == 3)
                    {
                        moneyReturned = (int) Math.Ceiling(bet * 1.5f);
                    }
                }
                if (score.ContainsKey("🍓"))
                {
                    if (score["🍓"] == 2)
                    {
                        moneyReturned = (int) Math.Ceiling(bet * 1f);
                    }
                    else if (score["🍓"] == 3)
                    {
                        moneyReturned = (int) Math.Ceiling(bet * 2f);
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
                        moneyReturned = (int) Math.Ceiling(bet * 4f);
                    }
                }
                if (score.ContainsKey("🍇"))
                {
                    if (score["🍇"] == 2)
                    {
                        moneyReturned = (int)Math.Ceiling(bet * 1.2f);
                    }
                    if (score["🍇"] == 3)
                    {
                        moneyReturned = (int) Math.Ceiling(bet * 6f);
                    }
                }
                if (score.ContainsKey("🍉"))
                {
                    if (score["🍉"] == 2)
                    {
                        moneyReturned = (int)Math.Ceiling(bet * 1.5f);
                    }
                    if (score["🍉"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(bet * 10f);
                    }
                }
                if (score.ContainsKey("⭐"))
                {
                    if (score["⭐"] == 2)
                    {
                        moneyReturned = (int)Math.Ceiling(bet * 2f);
                    }
                    if (score["⭐"] == 3)
                    {
                        moneyReturned = (int) Math.Ceiling(bet * 12f);
                    }
                }

                if (moneyReturned == 0)
                {
                    moneyReturned = -bet;
                    embed.AddField(locale.GetString("miki_module_fun_slots_lose_header"),
                        locale.GetString("miki_module_fun_slots_lose_amount", bet, u.Currency - bet));
                }
                else
                {
                    embed.AddField(locale.GetString(Locale.SlotsWinHeader),
                        locale.GetString(Locale.SlotsWinMessage, moneyReturned, u.Currency + moneyReturned));
                }

                embed.Description = string.Join(" ", objectsChosen);    
                await u.AddCurrencyAsync(moneyReturned, e.Channel);
                await context.SaveChangesAsync();

                await embed.SendToChannel(e.Channel);
            }
        }

        public async Task ValidateBet(EventContext e, Func<EventContext, int, Task> callback = null, int maxBet = 1000000)
        {
            if (!string.IsNullOrEmpty(e.arguments))
            {
                int bet;
                const int noAskLimit = 10000;

                using (MikiContext context = new MikiContext())
                {
                    User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                    if (user == null)
                    {
                        // TODO: add user null error
                        return;
                    }

                    string checkArg = e.arguments.Split(' ')[0];

                    if (int.TryParse(checkArg, out bet))
                    {

                    }
                    else if (checkArg.ToLower() == "all" || e.arguments == "*")
                    {
                        bet = user.Currency;
                    }
                    else
                    {
                        await e.ErrorEmbed(e.GetResource("miki_error_gambling_parse_error"))
                            .SendToChannel(e.Channel);
                        return;
                    }

                    if (bet < 1)
                    {
                        await e.ErrorEmbed(e.GetResource("miki_error_gambling_zero_or_less"))
                            .SendToChannel(e.Channel);
                    }
                    else if (bet > user.Currency)
                    {
                        await e.ErrorEmbed(e.GetResource("miki_mekos_insufficient"))
                            .SendToChannel(e.Channel);
                    }
                    else if (bet >= maxBet)
                    {
                        await e.ErrorEmbed($"you cannot bet more than {maxBet} mekos!")
                            .SendToChannel(e.Channel);
                        return;
                    }
                    else if (bet >= noAskLimit)
                    {
                        IDiscordEmbed embed = Utils.Embed;
                        embed.Description =
                            $"Are you sure you want to bet **{bet}**? You currently have `{user.Currency}` mekos.\n\nType `yes` to confirm.";
                        embed.Color = new IA.SDK.Color(0.4f, 0.6f, 1f);
                        await embed.SendToChannel(e.Channel);

                        CommandHandler confirmCommand = new CommandHandlerBuilder(Bot.instance.Events)
                            .AddPrefix("")
                            .DisposeInSeconds(20)
                            .SetOwner(e.message)
                            .AddCommand(
                                new RuntimeCommandEvent("yes")
                                    .Default(async (ec) =>
                                    {
                                        await ec.commandHandler.RequestDisposeAsync();
                                        await ec.message.DeleteAsync();
                                        if (callback != null)
                                        {
                                            await callback(e, bet);
                                        }
                                    })).Build();

                        Bot.instance.Events.AddPrivateCommandHandler(e.message, confirmCommand);
                    }
                    else
                    {
                        if (callback != null)
                        {
                            await callback(e, bet);
                        }
                    }
                }
            }
            else
            {
                await Utils.ErrorEmbed(e.Channel.GetLocale(), e.GetResource("miki_error_gambling_no_arg"))
                    .SendToChannel(e.Channel);
            }
        }
    }
}