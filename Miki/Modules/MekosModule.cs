using Discord;
using IA;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Languages;
using Miki.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module("Mekos")]
    public class MekosModule
    {
        [Command(Name = "mekos", Aliases = new string[] {"bal", "meko"})]
        public async Task ShowMekosAsync(EventContext e)
        {
            ulong targetId = e.message.MentionedUserIds.Count > 0 ? e.message.MentionedUserIds.First() : 0;

            if (e.message.MentionedUserIds.Count > 0)
            {
                if (targetId == 0)
                {
                    await e.ErrorEmbed(e.GetResource("miki_module_accounts_mekos_no_user")).SendToChannel(e.Channel);
                    return;
                }
                IDiscordUser userCheck = await e.Guild.GetUserAsync(targetId);
                if (userCheck.IsBot)
                {
                    await e.ErrorEmbed(e.GetResource("miki_module_accounts_mekos_bot")).SendToChannel(e.Channel);
                    return;
                }
            }

            using (var context = new MikiContext())
            {
                User user = await context.Users.FindAsync(targetId != 0 ? (long) targetId : e.Author.Id.ToDbLong());

                IDiscordEmbed embed = new RuntimeEmbed(new EmbedBuilder());
                embed.Title = "🔸 Mekos";
                embed.Description = e.GetResource("miki_user_mekos", user.Name, user.Currency);
                embed.Color = new IA.SDK.Color(1f, 0.5f, 0.7f);

                await embed.SendToChannel(e.Channel);
            }
        }

        [Command(Name = "give")]
        public async Task GiveMekosAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Guild.Id);

            string[] arguments = e.arguments.Split(' ');

            if (arguments.Length < 2)
            {
                await Utils.ErrorEmbed(locale, "give_error_no_arg").SendToChannel(e.Channel);
                return;
            }

            if (e.message.MentionedUserIds.Count <= 0)
            {
                await Utils.ErrorEmbed(locale, e.GetResource("give_error_no_mention")).SendToChannel(e.Channel);
                return;
            }

            if (!int.TryParse(arguments[1], out int goldSent))
            {
                await Utils.ErrorEmbed(locale, e.GetResource("give_error_amount_unparsable")).SendToChannel(e.Channel);
                return;
            }

            if (goldSent > 999999)
            {
                await Utils.ErrorEmbed(locale, e.GetResource("give_error_max_mekos")).SendToChannel(e.Channel);
                return;
            }

            if (goldSent <= 0)
            {
                await Utils.ErrorEmbed(locale, e.GetResource("give_error_min_mekos")).SendToChannel(e.Channel);
                return;
            }

            using (MikiContext context = new MikiContext())
            {
                User sender = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                if (sender == null)
                {
                    // HOW THE FUCK?!
                    return;
                }

                User receiver = await context.Users.FindAsync(e.message.MentionedUserIds.First().ToDbLong());

                if (receiver == null)
                {
                    await Utils.ErrorEmbed(locale, e.GetResource("user_error_no_account"))
                        .SendToChannel(e.Channel);
                    return;
                }

                if (goldSent <= sender.Currency)
                {
                    await receiver.AddCurrencyAsync(goldSent, e.Channel, sender);
                    await sender.AddCurrencyAsync(-goldSent, e.Channel, sender);

                    IDiscordEmbed em = Utils.Embed;
                    em.Title = "🔸 transaction";
                    em.Description = e.GetResource("give_description", sender.Name, receiver.Name, goldSent);

                    em.Color = new IA.SDK.Color(255, 140, 0);

                    await context.SaveChangesAsync();
                    await em.SendToChannel(e.Channel);
                }
                else
                {
                    await Utils.ErrorEmbed(locale, e.GetResource("user_error_insufficient_mekos"))
                        .SendToChannel(e.Channel);
                }
            }
        }

        [Command(Name = "daily")]
        public async Task GetDailyAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                if (u == null)
                {
                    await Utils.ErrorEmbed(locale, e.GetResource("user_error_no_account"))
                        .SendToChannel(e.Channel);
                    return;
                }

                int dailyAmount = 100;

                if (u.IsDonator(context))
                {
                    dailyAmount *= 2;
                }

                if (u.LastDailyTime.AddHours(23) >= DateTime.Now)
                {
                    await e.Channel.SendMessage(
                        $"You already claimed your daily today! Please wait another `{(u.LastDailyTime.AddHours(23) - DateTime.Now).ToTimeString(e.Channel.GetLocale())}` before using it again.");
                    return;
                }

                await u.AddCurrencyAsync(dailyAmount, e.Channel);
                u.LastDailyTime = DateTime.Now;

                await Utils.Embed
                    .SetTitle(locale.GetString("Daily"))
                    .SetDescription($"Received **{dailyAmount}** Mekos! You now have `{u.Currency}` Mekos")
                    .SendToChannel(e.Channel);

                await context.SaveChangesAsync();
            }
        }
    }
}

