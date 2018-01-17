using Discord;
using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Languages;
using Miki.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module(Name = "Marriage")]
    public class MarriageModule
    {
        [Command(Name = "marry")]
        public async Task MarryAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id);

            if (e.message.MentionedUserIds.Count == 0)
            {
                await e.Channel.SendMessage(locale.GetString("miki_module_accounts_marry_error_no_mention"));
                return;
            }

            using (MikiContext context = new MikiContext())
            {
				User mentionedPerson = await User.GetAsync(context, await e.Guild.GetUserAsync(e.message.MentionedUserIds.First()));
				User currentUser = await User.GetAsync(context, e.Author);

				if(mentionedPerson.Banned)
				{
					return;
				}

                IDiscordUser user = await e.Guild.GetUserAsync(e.message.MentionedUserIds.First());

                if (currentUser == null || mentionedPerson == null)
                {
                    await Utils.ErrorEmbed(locale, "miki_module_accounts_marry_error_null").SendToChannel(e.Channel);
                    return;
                }

                if (mentionedPerson.Id == currentUser.Id)
                {
                    await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_marry_error_null")).SendToChannel(e.Channel);
                    return;
                }

                if (await Marriage.ExistsAsync(context, mentionedPerson.Id, currentUser.Id))
                {
                    await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_marry_error_exists")).SendToChannel(e.Channel);
                    return;
                }

                if (await Marriage.ProposeAsync(context, currentUser.Id, mentionedPerson.Id))
                {
					await Utils.Embed
						.SetTitle("💍" + e.GetResource("miki_module_accounts_marry_text", $"**{e.Author.Username}**", $"**{user.Username}**"))
						.SetDescription(locale.GetString("miki_module_accounts_marry_text2", user.Username, e.Author.Username))
						.SetColor(0.4f, 0.4f, 0.8f)
						.SetThumbnailUrl("https://i.imgur.com/TKZSKIp.png")
						.AddInlineField("✅ To accept", $">acceptmarriage @user")
						.AddInlineField("❌ To decline", $">declinemarriage @user")
						.SetFooter("Take your time though! This proposal won't disappear", "")
						.SendToChannel(e.Channel);
				}
            }
        }

        private async Task ConfirmBuyMarriageSlot(EventContext cont, int costForUpgrade)
        {
            using (var context = new MikiContext())
            {
                User user = await User.GetAsync(context, cont.Author);

                if (user.Currency >= costForUpgrade)
                {
                    user.MarriageSlots++;
                    user.Currency -= costForUpgrade;
                    IDiscordEmbed notEnoughMekosErrorEmbed = new RuntimeEmbed(new EmbedBuilder());
                    notEnoughMekosErrorEmbed.Color = new IA.SDK.Color(0.4f, 1f, 0.6f);
                    notEnoughMekosErrorEmbed.Description = cont.GetResource("buymarriageslot_success", user.MarriageSlots);
                    await notEnoughMekosErrorEmbed.SendToChannel(cont.Channel);
                    await context.SaveChangesAsync();
                    await cont.commandHandler.RequestDisposeAsync();
                }
                else
                {
                    IDiscordEmbed notEnoughMekosErrorEmbed = new RuntimeEmbed(new EmbedBuilder());
                    notEnoughMekosErrorEmbed.Color = new IA.SDK.Color(1, 0.4f, 0.6f);
                    notEnoughMekosErrorEmbed.Description = cont.GetResource("buymarriageslot_insufficient_mekos", (costForUpgrade - user.Currency));
                    await notEnoughMekosErrorEmbed.SendToChannel(cont.Channel);
                    await cont.commandHandler.RequestDisposeAsync();
                }
            }
        }

        [Command(Name = "divorce")]
        public async Task DivorceAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if (e.message.MentionedUserIds.Count == 0)
            {
                using (MikiContext context = new MikiContext())
                {
                    List<User> users = context.Users.Where(p => p.Name.ToLower() == e.arguments.ToLower()).ToList();

                    if (users.Count == 0)
                    {
                        await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_no_marriage")).SendToChannel(e.Channel);
                    }
                    else if (users.Count == 1)
                    {
                        Marriage currentMarriage = Marriage.GetMarriage(context, e.Author.Id, users.First().Id.FromDbLong());
                        if (currentMarriage == null)
                        {
                            await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_no_marriage")).SendToChannel(e.Channel);
                            return;
                        }

                        if (currentMarriage.Proposing)
                        {
                            await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_no_marriage")).SendToChannel(e.Channel);
                            return;
                        }

                        await currentMarriage.DivorceAsync(context);

                        IDiscordEmbed embed = Utils.Embed;
                        embed.Title = locale.GetString("miki_module_accounts_divorce_header");
                        embed.Description = locale.GetString("miki_module_accounts_divorce_content", e.Author.Username, users.First().Name);
                        embed.Color = new IA.SDK.Color(0.6f, 0.4f, 0.1f);
                        await embed.SendToChannel(e.Channel);
                        return;
                    }
                    else
                    {
                        List<Marriage> allMarriages = await Marriage.GetMarriages(context, e.Author.Id.ToDbLong());
                        bool done = false;

                        foreach (Marriage marriage in allMarriages)
                        {
                            foreach (User user in users)
                            {
                                if (marriage.GetOther(e.Author.Id) == user.Id.FromDbLong())
                                {
                                    await marriage.DivorceAsync(context);
                                    done = true;

                                    IDiscordEmbed embed = Utils.Embed;
                                    embed.Title = locale.GetString("miki_module_accounts_divorce_header");
                                    embed.Description = locale.GetString("miki_module_accounts_divorce_content", e.Author.Username, user.Name);
                                    embed.Color = new IA.SDK.Color(0.6f, 0.4f, 0.1f);
                                    await embed.SendToChannel(e.Channel);
                                    break;
                                }
                            }

                            if (done)
                                break;
                        }
                    }
                }
            }
            else
            {
                if (e.Author.Id == e.message.MentionedUserIds.First())
                {
                    await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_no_marriage")).SendToChannel(e.Channel);
                    return;
                }

                using (MikiContext context = new MikiContext())
                {
                    Marriage currentMarriage = Marriage.GetMarriage(context, e.Author.Id, e.message.MentionedUserIds.First());

                    await currentMarriage.DivorceAsync(context);

                    string user1 = (await e.Guild.GetUserAsync(currentMarriage.GetMe(e.Author.Id))).Username;
                    string user2 = (await e.Guild.GetUserAsync(currentMarriage.GetOther(e.Author.Id))).Username;

                    IDiscordEmbed embed = Utils.Embed;
                    embed.Title = locale.GetString("miki_module_accounts_divorce_header");
                    embed.Description = locale.GetString("miki_module_accounts_divorce_content", user1, user2);
                    embed.Color = new IA.SDK.Color(0.6f, 0.4f, 0.1f);
                    await embed.SendToChannel(e.Channel);
                }
            }
        }

        [Command(Name = "acceptmarriage")]
        public async Task AcceptMarriageAsync(EventContext e)
        {
            if (e.message.MentionedUserIds.Count == 0)
            {
                await e.Channel.SendMessage("Please mention the person you want to marry.");
                return;
            }

            using (var context = new MikiContext())
            {
                Marriage marriage = await Marriage.GetProposalReceivedAsync(context, e.message.MentionedUserIds.First(), e.Author.Id);

                if (marriage != null)
                {
                    User person1 = await context.Users.FindAsync(marriage.Id1);
                    User person2 = await context.Users.FindAsync(marriage.Id2);

                    if (person1.MarriageSlots < (await Marriage.GetMarriages(context, person1.Id)).Count)
                    {
                        await e.Channel.SendMessage($"{person1.Name} do not have enough marriage slots, sorry :(");
                        return;
                    }

                    if (person2.MarriageSlots < (await Marriage.GetMarriages(context, person2.Id)).Count)
                    {
                        await e.Channel.SendMessage($"{person2.Name} does not have enough marriage slots, sorry :(");
                        return;
                    }

                    marriage.AcceptProposal(context);

                    Log.Message(marriage.Proposing.ToString());

                    await context.SaveChangesAsync();

                    await e.Channel.SendMessage($"❤️ Congratulations { person1.Name } and { person2.Name } ❤️");
                }
                else
                {
                    await e.Channel.SendMessage("This user hasn't proposed to you!");
                    return;
                }
            }
        }

        [Command(Name = "declinemarriage")]
        public async Task DeclineMarriageAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id);

            using (MikiContext context = new MikiContext())
            {
                if (e.arguments == "*")
                {
                    await Marriage.DeclineAllProposalsAsync(context, e.Author.Id.ToDbLong());
                    await e.Channel.SendMessage(locale.GetString("miki_marriage_all_declined"));
                    return;
                }

                if (e.message.MentionedUserIds.Count == 0)
                {
                    await e.Channel.SendMessage(locale.GetString("miki_marriage_no_mention"));
                    return;
                }

                Marriage marriage = await Marriage.GetEntryAsync(context, e.message.MentionedUserIds.First(), e.Author.Id);

                if (marriage != null)
                {
                    await marriage.DeclineProposalAsync(context);
                    await e.Channel.SendMessage(locale.GetString("miki_marriage_declined"));
                }
                else
                {
                    await e.Channel.SendMessage(locale.GetString("miki_marriage_null"));
                    return;
                }
            }
        }

        [Command(Name = "showproposals")]
        public async Task ShowProposalsAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                List<Marriage> proposals = Marriage.GetProposalsReceived(context, e.Author.Id.ToDbLong());
                List<string> proposalNames = new List<string>();

                foreach (Marriage p in proposals)
                {
                    User u = await context.Users.FindAsync(p.GetOther(e.Author.Id.ToDbLong()));
                    proposalNames.Add($"{u.Name} [{u.Id}]");
                }

                IDiscordEmbed embed = Utils.Embed;
                embed.Title = e.Author.Username;
                embed.Description = "Here it shows both the people who you've proposed to and who have proposed to you.";

                string output = string.Join("\n", proposalNames);

                embed.AddField("Proposals Recieved", string.IsNullOrEmpty(output) ? "none (yet!)" : output);

                proposals = Marriage.GetProposalsSent(context, e.Author.Id.ToDbLong());
                proposalNames = new List<string>();

                foreach (Marriage p in proposals)
                {
                    User u = await context.Users.FindAsync(p.GetOther(e.Author.Id.ToDbLong()));
                    proposalNames.Add($"{u.Name} [{u.Id}]");
                }

                output = string.Join("\n", proposalNames);

                embed.AddField("Proposals Sent", string.IsNullOrEmpty(output) ? "none (yet!)" : output);

                embed.Color = new IA.SDK.Color(1, 0.5f, 0);
                embed.ThumbnailUrl = (await e.Guild.GetUserAsync(e.Author.Id)).AvatarUrl;
                await embed.SendToChannel(e.Channel);
            }
        }

        [Command(Name = "buymarriageslot")]
        public async Task BuyMarriageSlotAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                int limit = 10;

                if (user.IsDonator(context))
                {
                    limit += 5;
                }

                IDiscordEmbed embed = new RuntimeEmbed(new EmbedBuilder());

                if (user.MarriageSlots >= limit)
                {
                    embed.Description = $"For now, **{limit} slots** is the max. sorry :(";

                    if (limit == 10 && !user.IsDonator(context))
                    {
						embed.AddField("Pro tip!", "Donators get 5 more slots!")
							.SetFooter("Check `>donate` for more information!", "");
                    }

                    embed.Color = new IA.SDK.Color(1f, 0.6f, 0.4f);
                    await embed.SendToChannel(e.Channel);
                    return;
                }

                int costForUpgrade = (user.MarriageSlots - 4) * 2500;

                embed.Description = $"Do you want to buy a marriage slot for **{costForUpgrade}**?\n\nType `yes` to confirm.";
                embed.Color = new IA.SDK.Color(0.4f, 0.6f, 1f);
                await embed.SendToChannel(e.Channel);

                CommandHandler c = new CommandHandlerBuilder()
                    .AddPrefix("")
                    .DisposeInSeconds(20)
                    .SetOwner(e.message)
                    .AddCommand(
                        new RuntimeCommandEvent("yes")
                            .Default(async (cont) =>
                            {
                                await ConfirmBuyMarriageSlot(cont, costForUpgrade);
                            }))
                            .Build();

                Bot.instance.Events.AddPrivateCommandHandler(e.message, c);
            }
        }
    }
}
