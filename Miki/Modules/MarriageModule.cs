using Discord;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Miki.Common.Events;
using Miki.Common.Interfaces;
using Miki.Languages;
using Miki.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Miki.Modules
{
    [Module(Name = "Marriage")]
    public class MarriageModule
    {
		[Command(Name = "marry")]
		public async Task MarryAsync(EventContext e)
		{
			Locale locale = new Locale(e.Channel.Id);

			if (e.message.MentionedUserIds.Count == 0)
			{
				e.Channel.QueueMessageAsync(locale.GetString("miki_module_accounts_marry_error_no_mention"));
				return;
			}

			long askerId = 0;
			long receiverId = 0;

			IDiscordUser user = await e.Guild.GetUserAsync(e.message.MentionedUserIds.First());

			using (MikiContext context = new MikiContext())
			{

				User mentionedPerson = await User.GetAsync(context, user);
				User currentUser = await User.GetAsync(context, e.Author);
				askerId = currentUser.Id;
				receiverId = mentionedPerson.Id;

				if (mentionedPerson.Banned)
				{
					return;
				}

				if (currentUser == null || mentionedPerson == null)
				{
					e.ErrorEmbed(locale.GetString("miki_module_accounts_marry_error_null")).QueueToChannel(e.Channel);
					return;
				}

				if (mentionedPerson.Id == currentUser.Id)
				{
					e.ErrorEmbed(locale.GetString("miki_module_accounts_marry_error_null")).QueueToChannel(e.Channel);
					return;
				}

				if (await Marriage.ExistsAsync(context, mentionedPerson.Id, currentUser.Id))
				{
					e.ErrorEmbed(locale.GetString("miki_module_accounts_marry_error_exists")).QueueToChannel(e.Channel);
					return;
				}
			}

			if (await Marriage.ProposeAsync(askerId, receiverId))
			{
				Utils.Embed
					.SetTitle("💍" + e.GetResource("miki_module_accounts_marry_text", $"**{e.Author.Username}**", $"**{user.Username}**"))
					.SetDescription(locale.GetString("miki_module_accounts_marry_text2", user.Username, e.Author.Username))
					.SetColor(0.4f, 0.4f, 0.8f)
					.SetThumbnailUrl("https://i.imgur.com/TKZSKIp.png")
					.AddInlineField("✅ To accept", $">acceptMarriage @user")
					.AddInlineField("❌ To decline", $">declineMarriage @user")
					.SetFooter("Take your time though! This proposal won't disappear", "")
					.QueueToChannel(e.Channel);
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
                    notEnoughMekosErrorEmbed.Color = new Miki.Common.Color(0.4f, 1f, 0.6f);
                    notEnoughMekosErrorEmbed.Description = cont.GetResource("buyMarriageslot_success", user.MarriageSlots);
                    notEnoughMekosErrorEmbed.QueueToChannel(cont.Channel);
                    await context.SaveChangesAsync();
                    await cont.commandHandler.RequestDisposeAsync();
                }
                else
                {
                    IDiscordEmbed notEnoughMekosErrorEmbed = new RuntimeEmbed(new EmbedBuilder());
                    notEnoughMekosErrorEmbed.Color = new Miki.Common.Color(1, 0.4f, 0.6f);
                    notEnoughMekosErrorEmbed.Description = cont.GetResource("buyMarriageslot_insufficient_mekos", (costForUpgrade - user.Currency));
                    notEnoughMekosErrorEmbed.QueueToChannel(cont.Channel);
                    await cont.commandHandler.RequestDisposeAsync();
                }
            }
        }

        [Command(Name = "divorce")]
        public async Task DivorceAsync(EventContext e)
        {
			Locale locale = new Locale(e.Channel.Id);

			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg != null)
            {
                using (MikiContext context = new MikiContext())
                {
                    List<User> users = context.Users.Where(p => p.Name.ToLower() == e.Arguments.ToString().ToLower()).ToList();

                    if (users.Count == 0)
                    {
                        e.ErrorEmbed(locale.GetString("miki_module_accounts_error_no_Marriage"))
							.QueueToChannel(e.Channel);
                    }
                    else if (users.Count == 1)
                    {
                        Marriage currentMarriage = await Marriage.GetMarriageAsync(context, e.Author.Id, users.First().Id.FromDbLong());
                        if (currentMarriage == null)
                        {
                            e.ErrorEmbed(locale.GetString("miki_module_accounts_error_no_Marriage"))
								.QueueToChannel(e.Channel);
                            return;
                        }

                        if (currentMarriage.IsProposing)
                        {
                            e.ErrorEmbed(locale.GetString("miki_module_accounts_error_no_Marriage"))
								.QueueToChannel(e.Channel);
                            return;
                        }

                        await currentMarriage.RemoveAsync(context);

                        IDiscordEmbed embed = Utils.Embed;
                        embed.Title = locale.GetString("miki_module_accounts_divorce_header");
                        embed.Description = locale.GetString("miki_module_accounts_divorce_content", e.Author.Username, users.First().Name);
                        embed.Color = new Common.Color(0.6f, 0.4f, 0.1f);
                        embed.QueueToChannel(e.Channel);
                        return;
                    }
                    else
                    {
                        List<Marriage> allMarriages = await Marriage.GetMarriagesAsync(context, e.Author.Id.ToDbLong());
                        bool done = false;

                        foreach (Marriage Marriage in allMarriages)
                        {
                            foreach (User user in users)
                            {
                                if (Marriage.GetOther(e.Author.Id) == user.Id.FromDbLong())
                                {
                                    await Marriage.RemoveAsync(context);
                                    done = true;

                                    IDiscordEmbed embed = Utils.Embed;
                                    embed.Title = locale.GetString("miki_module_accounts_divorce_header");
                                    embed.Description = locale.GetString("miki_module_accounts_divorce_content", e.Author.Username, user.Name);
                                    embed.Color = new Common.Color(0.6f, 0.4f, 0.1f);
                                    embed.QueueToChannel(e.Channel);
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
                    e.ErrorEmbed(locale.GetString("miki_module_accounts_error_no_Marriage"))
						.QueueToChannel(e.Channel);
                    return;
                }

                using (MikiContext context = new MikiContext())
                {
					User author = await User.GetAsync(context, e.Author);
					Marriage Marriage = author.Marriages
						.FirstOrDefault(x => x.Marriage.GetOther(author.Id) == e.message.MentionedUserIds.First().ToDbLong())?.Marriage;

					if (Marriage != null)
					{
						string user1 = (await e.Guild.GetUserAsync(Marriage.GetMe(e.Author.Id))).Username;
						string user2 = (await e.Guild.GetUserAsync(Marriage.GetOther(e.Author.Id))).Username;

						await Marriage.RemoveAsync(context);

						IDiscordEmbed embed = Utils.Embed;
						embed.Title = locale.GetString("miki_module_accounts_divorce_header");
						embed.Description = locale.GetString("miki_module_accounts_divorce_content", user1, user2);
						embed.Color = new Miki.Common.Color(0.6f, 0.4f, 0.1f);
						embed.QueueToChannel(e.Channel);
					}
                }
            }
        }

        [Command(Name = "acceptMarriage")]
        public async Task AcceptMarriageAsync(EventContext e)
        {
            if (e.message.MentionedUserIds.Count == 0)
            {
				e.ErrorEmbed("Please mention the person you want to marry.")
					.QueueToChannel(e.Channel);
                return;
            }

			if (e.message.MentionedUserIds.First() == e.Author.Id)
			{
				e.ErrorEmbed("Please mention someone else than yourself.")
					.QueueToChannel(e.Channel);
				return;
			}

            using (var context = new MikiContext())
            {
				User accepter = await User.GetAsync(context, e.Author);

				IDiscordUser user = await e.Guild.GetUserAsync(e.message.MentionedUserIds.First());
				User asker = await User.GetAsync(context, user);

				UserMarriedTo marriedTo = await context.UsersMarriedTo
						.Include(x => x.Marriage)
					.Where(x => x.UserId == accepter.Id)
					.FirstOrDefaultAsync();



                if (marriedTo.Marriage != null)
                {
                    if (accepter.MarriageSlots < (await Marriage.GetMarriagesAsync(context, accepter.Id)).Count)
                    {
                        e.Channel.QueueMessageAsync($"{e.Author.GetName()} do not have enough Marriage slots, sorry :(");
                        return;
                    }

                    if (asker.MarriageSlots < (await Marriage.GetMarriagesAsync(context, asker.Id)).Count)
                    {
                        e.Channel.QueueMessageAsync($"{asker.Name} does not have enough Marriage slots, sorry :(");
                        return;
                    }

					if (marriedTo.Marriage.IsProposing)
					{
						marriedTo.Marriage.AcceptProposal(context);

						await context.SaveChangesAsync();

						Utils.Embed
							.SetTitle("❤️ Happily married")
							.SetColor(190, 25, 49)
							.SetDescription($"Much love to { e.Author.GetName() } and { user.GetName() } in their future adventures together!")
							.QueueToChannel(e.Channel);
					}
					else
					{
						e.ErrorEmbed("You're already married to this person ya doofus!")
							.QueueToChannel(e.Channel);
					}
				}
                else
                {
                    e.Channel.QueueMessageAsync("This user hasn't proposed to you!");
                    return;
                }
            }
        }

        [Command(Name = "declineMarriage")]
        public async Task DeclineMarriageAsync(EventContext e)
        {
            Locale locale = new Locale(e.Channel.Id);

            using (MikiContext context = new MikiContext())
            {
				if (e.Arguments.ToString() == "*")
                {
                    await Marriage.DeclineAllProposalsAsync(context, e.Author.Id.ToDbLong());
                    e.Channel.QueueMessageAsync(locale.GetString("miki_Marriage_all_declined"));
                    return;
                }

                if (e.message.MentionedUserIds.Count == 0)
                {
                    e.Channel.QueueMessageAsync(locale.GetString("miki_Marriage_no_mention"));
                    return;
                }

				IDiscordUser user = await e.Guild.GetUserAsync(e.message.MentionedUserIds.First());

				Marriage marriage = await Marriage.GetEntryAsync(context, e.message.MentionedUserIds.First(), e.Author.Id);

				if (marriage == null)
				{
					e.Channel.QueueMessageAsync(locale.GetString("miki_Marriage_null"));
					return;
				}

				if (marriage.IsProposing)
				{
					await marriage.RemoveAsync(context);

					Utils.Embed.SetTitle($"🔫 You shot down {user.GetName()}!")
						.SetDescription($"Aww, don't worry {user.GetName()}. There is plenty of fish in the sea!")
						.SetColor(191, 105, 82)
						.QueueToChannel(e.Channel);
				}
				else
				{
					e.ErrorEmbed("You're already married to this person ya doofus!")
						.QueueToChannel(e.Channel);
				}
			}
        }

        [Command(Name = "showproposals")]
        public async Task ShowProposalsAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                List<Marriage> proposals = await Marriage.GetProposalsReceived(context, e.Author.Id.ToDbLong());
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

                proposals = await Marriage.GetProposalsSent(context, e.Author.Id.ToDbLong());
                proposalNames = new List<string>();

                foreach (Marriage p in proposals)
                {
                    User u = await context.Users.FindAsync(p.GetOther(e.Author.Id.ToDbLong()));
                    proposalNames.Add($"{u.Name} [{u.Id}]");
                }

                output = string.Join("\n", proposalNames);

                embed.AddField("Proposals Sent", string.IsNullOrEmpty(output) ? "none (yet!)" : output);

                embed.Color = new Miki.Common.Color(1, 0.5f, 0);
                embed.ThumbnailUrl = (await e.Guild.GetUserAsync(e.Author.Id)).AvatarUrl;
                embed.QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "buyMarriageslot")]
        public async Task BuyMarriageSlotAsync(EventContext e)
        {
			using (var context = new MikiContext())
			{
				User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				int limit = 10;
				bool isDonator = await user.IsDonatorAsync(context);

				if (isDonator)
				{
					limit += 5;
				}

				IDiscordEmbed embed = new RuntimeEmbed(new EmbedBuilder());

				if (user.MarriageSlots >= limit)
				{
					embed.Description = $"For now, **{limit} slots** is the max. sorry :(";

					if (limit == 10 && !isDonator)
					{
						embed.AddField("Pro tip!", "Donators get 5 more slots!")
							.SetFooter("Check `>donate` for more information!", "");
					}

					embed.Color = new Common.Color(1f, 0.6f, 0.4f);
					embed.QueueToChannel(e.Channel);
					return;
				}

				int costForUpgrade = (user.MarriageSlots - 4) * 2500;

				embed.Description = $"Do you want to buy a Marriage slot for **{costForUpgrade}**?\n\nType `yes` to confirm.";
				embed.Color = new Miki.Common.Color(0.4f, 0.6f, 1f);
				embed.QueueToChannel(e.Channel);

				CommandHandler c = new CommandHandlerBuilder(EventSystem.Instance)
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

				EventSystem.Instance.AddPrivateCommandHandler(e.message, c);
			}
        }
    }
}
