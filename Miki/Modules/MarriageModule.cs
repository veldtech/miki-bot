using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Miki.Languages;
using Miki.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Miki.Framework.Extension;
using System.Text;
using Miki.Framework.Exceptions;
using System;
using Miki.Framework.Events.Commands;
using Miki.Framework.Languages;
using Miki.Discord.Rest;
using Miki.Discord;
using Miki.Discord.Common;

namespace Miki.Modules
{
    [Module(Name = "Marriage")]
    public class MarriageModule
    {
		[Command(Name = "marry")]
		public async Task MarryAsync(EventContext e)
		{
			long askerId = 0;
			long receiverId = 0;

			ArgObject args = e.Arguments.FirstOrDefault();

			if (args == null)
				return;

			IDiscordGuildUser user = await args.GetUserAsync(e.Guild);

			if (user == null)
			{
				e.Channel.QueueMessageAsync("Couldn't find this person..");
				return;
			}

			if (user.Id == (await e.Guild.GetSelfAsync()).Id)
			{
				e.Channel.QueueMessageAsync("(´・ω・`)");
				return;
			}

			using (MikiContext context = new MikiContext())
			{
				User mentionedPerson = await User.GetAsync(context, user);
				User currentUser = await User.GetAsync(context, e.Author);

				askerId = currentUser.Id;
				receiverId = mentionedPerson.Id;

				if (currentUser == null || mentionedPerson == null)
				{
					e.ErrorEmbed(e.GetResource("miki_module_accounts_marry_error_null")).ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				if (mentionedPerson.Banned)
				{
					e.ErrorEmbed("This person has been banned from Miki.").ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				if (mentionedPerson.Id == currentUser.Id)
				{
					e.ErrorEmbed(e.GetResource("miki_module_accounts_marry_error_null")).ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				if (await Marriage.ExistsAsync(context, mentionedPerson.Id, currentUser.Id))
				{
					e.ErrorEmbed(e.GetResource("miki_module_accounts_marry_error_exists")).ToEmbed().QueueToChannel(e.Channel);
					return;
				}
			}

			await Marriage.ProposeAsync(askerId, receiverId);

			Utils.Embed
				.SetTitle("💍" + e.GetResource("miki_module_accounts_marry_text", $"**{e.Author.Username}**", $"**{user.Username}**"))
				.SetDescription(e.GetResource("miki_module_accounts_marry_text2", user.Username, e.Author.Username))
				.SetColor(0.4f, 0.4f, 0.8f)
				.SetThumbnail("https://i.imgur.com/TKZSKIp.png")
				.AddInlineField("✅ To accept", $">acceptmarriage @user")
				.AddInlineField("❌ To decline", $">declinemarriage @user")
				.SetFooter("Take your time though! This proposal won't disappear", "")
				.ToEmbed().QueueToChannel(e.Channel);
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

					new EmbedBuilder()
					{
						Color = new Color(0.4f, 1f, 0.6f),
						Description = cont.GetResource("buymarriageslot_success", user.MarriageSlots),
					}.ToEmbed().QueueToChannel(cont.Channel);

					await context.SaveChangesAsync();

					await cont.EventSystem.GetCommandHandler<SessionBasedCommandHandler>().RemoveSessionAsync(cont.Author.Id, cont.Channel.Id);
				}
				else
				{
					new EmbedBuilder()
					{
						Color = new Color(1, 0.4f, 0.6f),
						Description = cont.GetResource("buymarriageslot_insufficient_mekos", (costForUpgrade - user.Currency)),
					}.ToEmbed().QueueToChannel(cont.Channel);
					await cont.EventSystem.GetCommandHandler<SessionBasedCommandHandler>().RemoveSessionAsync(cont.Author.Id, cont.Channel.Id);
				}
			}
        }

		[Command(Name = "divorce")]
		public async Task DivorceAsync(EventContext e)
		{
			using (MikiContext context = new MikiContext())
			{
				var marriages = await Marriage.GetMarriagesAsync(context, e.Author.Id.ToDbLong());

				if (marriages.Count == 0)
					throw new Exception("You're not married to anyone.");

				UserMarriedTo m = await SelectMarriageAsync(e, context, marriages);

				string otherName = await User.GetNameAsync(context, m.GetOther(e.Author.Id.ToDbLong()));

				EmbedBuilder embed = Utils.Embed;
				embed.Title = $"🔔 {e.GetResource("miki_module_accounts_divorce_header")}";
				embed.Description = e.GetResource("miki_module_accounts_divorce_content", e.Author.Username, otherName);
				embed.Color = new Color(0.6f, 0.4f, 0.1f);
				embed.ToEmbed().QueueToChannel(e.Channel);

				m.Remove(context);
				await context.SaveChangesAsync();
			}
		}

		[Command(Name = "acceptmarriage")]
        public async Task AcceptMarriageAsync(EventContext e)
        {
			IDiscordUser user = await e.Arguments.Join().GetUserAsync(e.Guild);

			if(user == null)
			{
				e.ErrorEmbed("I couldn't find this user!")
					.ToEmbed().QueueToChannel(e.Channel);
			}

			if (user.Id == e.Author.Id)
			{
				e.ErrorEmbed("Please mention someone else than yourself.")
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

            using (var context = new MikiContext())
            {
				User accepter = await User.GetAsync(context, e.Author);
				User asker = await User.GetAsync(context, user);

				UserMarriedTo marriage = await Marriage.GetEntryAsync(context, accepter.Id, asker.Id);

                if (marriage != null)
                {
                    if (accepter.MarriageSlots < (await Marriage.GetMarriagesAsync(context, accepter.Id)).Count)
                    {
                        e.Channel.QueueMessageAsync($"{e.Author.Username} do not have enough Marriage slots, sorry :(");
                        return;
                    }

                    if (asker.MarriageSlots < (await Marriage.GetMarriagesAsync(context, asker.Id)).Count)
                    {
                        e.Channel.QueueMessageAsync($"{asker.Name} does not have enough Marriage slots, sorry :(");
                        return;
                    }

					if(marriage.ReceiverId != e.Author.Id.ToDbLong())
					{
						e.Channel.QueueMessageAsync($"You can not accept your own responses!");
						return;
					}

					if (marriage.Marriage.IsProposing)
					{
						marriage.Marriage.AcceptProposal(context);

						await context.SaveChangesAsync();

						new EmbedBuilder()
						{ 
							Title = ("❤️ Happily married"),
							Color = new Color(190, 25, 49),
							Description = ($"Much love to { e.Author.Username } and { user.Username } in their future adventures together!")
						}.ToEmbed().QueueToChannel(e.Channel);
					}
					else
					{
						e.ErrorEmbed("You're already married to this person ya doofus!")
							.ToEmbed().QueueToChannel(e.Channel);
					}
				}
                else
                {
                    e.Channel.QueueMessageAsync("This user hasn't proposed to you!");
                    return;
                }
            }
        }

        [Command(Name = "declinemarriage")]
        public async Task DeclineMarriageAsync(EventContext e)
        {
			using (MikiContext context = new MikiContext())
			{
				var marriages = await Marriage.GetProposalsReceived(context, e.Author.Id.ToDbLong());

				if (marriages.Count == 0)
					throw new Exception("You do not have any proposals.");

				UserMarriedTo m = await SelectMarriageAsync(e, context, marriages);

				string otherName = await User.GetNameAsync(context, m.GetOther(e.Author.Id.ToDbLong()));

				new EmbedBuilder()
				{
					Title = $"🔫 You shot down {otherName}!",
					Description = $"Aww, don't worry {otherName}. There is plenty of fish in the sea!",
					Color = new Color(191, 105, 82)
				}.ToEmbed().QueueToChannel(e.Channel);

				m.Remove(context);
				await context.SaveChangesAsync();
			}
        }

        [Command(Name = "showproposals")]
        public async Task ShowProposalsAsync(EventContext e)
        {
			int page = e.Arguments.FirstOrDefault()?.AsInt() - 1 ?? 0;

            using (var context = new MikiContext())
            {
                List<UserMarriedTo> proposals = await Marriage.GetProposalsReceived(context, e.Author.Id.ToDbLong());
                List<string> proposalNames = new List<string>();

                foreach (UserMarriedTo p in proposals)
                {
					long id = p.GetOther(e.Author.Id.ToDbLong());
					string u = await User.GetNameAsync(context, id);
                    proposalNames.Add($"{u} [{id}]");
                }

				int pageCount = (int)Math.Ceiling((float)proposalNames.Count / 35);

				proposalNames = proposalNames.Skip(page * 35)
					.Take(35)
					.ToList();

				EmbedBuilder embed = new EmbedBuilder()
					.SetTitle(e.Author.Username)
					.SetDescription("Here it shows both the people who you've proposed to and who have proposed to you.");

                string output = string.Join("\n", proposalNames);

                embed.AddField("Proposals Recieved", string.IsNullOrEmpty(output) ? "none (yet!)" : output);

                proposals = await Marriage.GetProposalsSent(context, e.Author.Id.ToDbLong());
                proposalNames = new List<string>();

                foreach (UserMarriedTo p in proposals)
                {
					long id = p.GetOther(e.Author.Id.ToDbLong());
					string u = await User.GetNameAsync(context, id);
                    proposalNames.Add($"{u} [{id}]");
                }

				pageCount = Math.Max(pageCount, (int)Math.Ceiling((float)proposalNames.Count / 35));

				proposalNames = proposalNames.Skip(page * 35)
					.Take(35)
					.ToList();

				output = string.Join("\n", proposalNames);

                embed.AddField("Proposals Sent", string.IsNullOrEmpty(output) ? "none (yet!)" : output);

                embed.Color = new Color(1, 0.5f, 0);
                embed.ThumbnailUrl = (await e.Guild.GetUserAsync(e.Author.Id)).GetAvatarUrl();
				if (pageCount > 1)
				{
					embed.SetFooter(Locale.GetString(e.Channel.Id, "page_footer", page + 1, pageCount));
				}
				embed.ToEmbed().QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "buymarriageslot")]
        public async Task BuyMarriageSlotAsync(EventContext e)
        {
			using (var context = new MikiContext())
			{
				User user = await User.GetAsync(context, e.Author);

				int limit = 10;
				bool isDonator = await user.IsDonatorAsync(context);

				if (isDonator)
				{
					limit += 5;
				}

				EmbedBuilder embed = new EmbedBuilder();

				if (user.MarriageSlots >= limit)
				{
					embed.Description = $"For now, **{limit} slots** is the max. sorry :(";

					if (limit == 10 && !isDonator)
					{
						embed.AddField("Pro tip!", "Donators get 5 more slots!")
							.SetFooter("Check `>donate` for more information!", "");
					}

					embed.Color = new Color(1f, 0.6f, 0.4f);
					embed.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				int costForUpgrade = (user.MarriageSlots - 4) * 2500;

				embed.Description = $"Do you want to buy a Marriage slot for **{costForUpgrade}**?\n\nType `yes` to confirm.";
				embed.Color = new Color(0.4f, 0.6f, 1f);

				SimpleCommandHandler commandHandler = new SimpleCommandHandler(new CommandMap());
				commandHandler.AddPrefix("");
				commandHandler.AddCommand(new CommandEvent("yes")
					.Default(async (cx) => await ConfirmBuyMarriageSlot(cx, costForUpgrade)));

				await e.EventSystem.GetCommandHandler<SessionBasedCommandHandler>().AddSessionAsync(e.CreateSession(), commandHandler, new TimeSpan(0, 0, 20));

				embed.ToEmbed().QueueToChannel(e.Channel);
			}
        }

		private async Task<UserMarriedTo> SelectMarriageAsync(EventContext e, MikiContext context, List<UserMarriedTo> marriages)
		{
			EmbedBuilder embed = new EmbedBuilder()
			{
				Title = "💔  Select marriage to divorce",
				Description = "Please type in the number of which marriage you want to divorce.",
				Color = new Color(231, 90, 112)
			};

			var m = marriages.OrderBy(x => x.Marriage.TimeOfMarriage);

			StringBuilder builder = new StringBuilder();
			
			for(int i = 0; i < m.Count(); i++)
				builder.AppendLine($"`{(i+1).ToString().PadLeft(2)}:` {await User.GetNameAsync(context, m.ElementAt(i).GetOther(e.Author.Id.ToDbLong()))}");

			embed.Description += "\n\n" + builder.ToString();

			embed.ToEmbed().QueueToChannel(e.Channel);

			IDiscordMessage msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());

			if (int.TryParse(msg.Content, out int response))
			{
				if(response > 0 && response <= marriages.Count)
				{
					return m.ElementAt(response - 1);
				}
				throw new Exception("This number is not listed, cancelling divorce.");
			}
			throw new Exception("This is not a number, cancelling divorce.");
		}
    }
}
