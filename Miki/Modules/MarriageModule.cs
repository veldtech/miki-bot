using Miki.Bot.Models.Repositories;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Exceptions;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Framework.Events.Commands;
using Miki.Helpers;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module(Name = "Marriage")]
	public class MarriageModule
    {
        [Command(Name = "buymarriageslot")]
        public async Task BuyMarriageSlotAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                User user = await DatabaseHelpers.GetUserAsync(context, e.Author);

                int limit = 10;
                bool isDonator = await user.IsDonatorAsync(context);

                if (isDonator)
                {
                    limit += 5;
                }

                if (user.MarriageSlots >= limit)
                {
                    EmbedBuilder embed = Utils.ErrorEmbed(e, $"For now, **{limit} slots** is the max. sorry :(");

                    if (limit == 10 && !isDonator)
                    {
                        embed.AddField("Pro tip!", "Donators get 5 more slots!")
                            .SetFooter("Check `>donate` for more information!");
                    }

                    embed.Color = new Color(1f, 0.6f, 0.4f);
                    await embed.ToEmbed().QueueToChannelAsync(e.Channel);
                    return;
                }

                int costForUpgrade = (user.MarriageSlots - 4) * 2500;

                user.MarriageSlots++;
                user.RemoveCurrency(costForUpgrade);

                await new EmbedBuilder()
                {
                    Color = new Color(0.4f, 1f, 0.6f),
                    Description = e.Locale.GetString("buymarriageslot_success", user.MarriageSlots),
                }.ToEmbed().QueueToChannelAsync(e.Channel);

                await context.SaveChangesAsync();

            }
        }

		[Command(Name = "acceptmarriage")]
		public async Task AcceptMarriageAsync(EventContext e)
		{
			IDiscordUser user = await DiscordExtensions.GetUserAsync(e.Arguments.Pack.TakeAll(), e.Guild);

			if (user == null)
			{
				await e.ErrorEmbed("I couldn't find this user!")
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

			if (user.Id == e.Author.Id)
			{
				await e.ErrorEmbed("Please mention someone else than yourself.")
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

			using (var context = new MikiContext())
			{
				MarriageRepository repository = new MarriageRepository(context);

				User accepter = await DatabaseHelpers.GetUserAsync(context, e.Author);
				User asker = await DatabaseHelpers.GetUserAsync(context, user);

				UserMarriedTo marriage = await repository.GetEntryAsync(accepter.Id, asker.Id);

				if (marriage != null)
				{
					if (accepter.MarriageSlots < (await repository.GetMarriagesAsync(accepter.Id)).Count)
					{
						throw new InsufficientMarriageSlotsException(accepter);
					}

					if (asker.MarriageSlots < (await repository.GetMarriagesAsync(asker.Id)).Count)
					{
						throw new InsufficientMarriageSlotsException(asker);
					}

					if (marriage.ReceiverId != e.Author.Id.ToDbLong())
					{
						e.Channel.QueueMessage($"You can not accept your own responses!");
						return;
					}

					if (marriage.Marriage.IsProposing)
					{
						marriage.Marriage.AcceptProposal();

						await context.SaveChangesAsync();

						await new EmbedBuilder()
						{
							Title = ("❤️ Happily married"),
							Color = new Color(190, 25, 49),
							Description = ($"Much love to { e.Author.Username } and { user.Username } in their future adventures together!")
						}.ToEmbed().QueueToChannelAsync(e.Channel);
					}
					else
					{
						await e.ErrorEmbed("You're already married to this person ya doofus!")
							.ToEmbed().QueueToChannelAsync(e.Channel);
					}
				}
				else
				{
					e.Channel.QueueMessage("This user hasn't proposed to you!");
					return;
				}
			}
		}

        [Command(Name = "cancelmarriage")]
        public async Task CancelMarriageAsync(EventContext e)
        {
            using (MikiContext context = new MikiContext())
            {
                MarriageRepository repository = new MarriageRepository(context);
          
                var marriages = await repository.GetProposalsSent(e.Author.Id.ToDbLong());

                if (marriages.Count == 0)
                {
                    // TODO: add no propsoals
                    //throw new LocalizedException("error_proposals_empty");
                    return;
                }

                marriages = marriages.OrderByDescending(x => x.Marriage.TimeOfMarriage).ToList();

                if (e.Arguments.Take(out int selectionId))
                {
                    var m = marriages[selectionId - 1];
                    string otherName = (await MikiApp.Instance.Discord.GetUserAsync(m.GetOther(e.Author.Id.ToDbLong()).FromDbLong())).Username;

                    await new EmbedBuilder()
                    {
                        Title = $"💔 You took back your proposal to {otherName}!",
                        Description = $"Aww, don't worry {otherName}. There is plenty of fish in the sea!",
                        Color = new Color(231, 90, 112)
                    }.ToEmbed().QueueToChannelAsync(e.Channel);

                    m.Remove(context);
                    await context.SaveChangesAsync();
                }
                else
                {
                    var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));

                    var embed = new EmbedBuilder()
                    {
                        Title = "💍 Proposals",
                        Footer = new EmbedFooter()
                        {
                            Text = $"Use {await e.Prefix.GetForGuildAsync(context, cache, e.Guild.Id)}cancelmarriage <number> to decline",
                        },
                        Color = new Color(154, 170, 180)
                    };

                    await BuildMarriageEmbedAsync(embed, e.Author.Id.ToDbLong(), context, marriages);

                    await embed.ToEmbed()
                        .QueueToChannelAsync(e.Channel);
                }
            }
        }

		[Command(Name = "declinemarriage")]
		public async Task DeclineMarriageAsync(EventContext e)
		{
			using (MikiContext context = new MikiContext())
			{
				MarriageRepository repository = new MarriageRepository(context);

				var marriages = await repository.GetProposalsReceived(e.Author.Id.ToDbLong());

				if (marriages.Count == 0)
				{
					// TODO: add no propsoals
					//throw new LocalizedException("error_proposals_empty");
					return;
				}

				marriages = marriages.OrderByDescending(x => x.Marriage.TimeOfMarriage).ToList();

                if (e.Arguments.Take(out int selectionId))
                {
                    var m = marriages[selectionId - 1];
					string otherName = (await MikiApp.Instance.Discord.GetUserAsync(m.GetOther(e.Author.Id.ToDbLong()).FromDbLong())).Username;

					await new EmbedBuilder()
					{
						Title = $"🔫 You shot down {otherName}!",
						Description = $"Aww, don't worry {otherName}. There is plenty of fish in the sea!",
						Color = new Color(191, 105, 82)
					}.ToEmbed().QueueToChannelAsync(e.Channel);

					m.Remove(context);
					await context.SaveChangesAsync();
				}
				else
				{
                    var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));

					var embed = new EmbedBuilder()
					{
						Title = "💍 Proposals",
						Footer = new EmbedFooter()
						{
							Text = $"Use {await e.Prefix.GetForGuildAsync(context, cache, e.Guild.Id)}declinemarriage <number> to decline",
						},
						Color = new Color(154, 170, 180)
					};

                    await BuildMarriageEmbedAsync(embed, e.Author.Id.ToDbLong(), context, marriages);
					await embed.ToEmbed().QueueToChannelAsync(e.Channel);
				}
			}
		}

        [Command(Name = "divorce")]
        public async Task DivorceAsync(EventContext e)
        {
            using (MikiContext context = new MikiContext())
            {
                MarriageRepository repository = new MarriageRepository(context);

                var marriages = await repository.GetMarriagesAsync((long)e.Author.Id);

                if (marriages.Count == 0)
                {
                    // TODO: no proposals exception
                    return;
                }

                marriages = marriages.OrderByDescending(x => x.Marriage.TimeOfMarriage).ToList();

                if (e.Arguments.Take(out int selectionId))
                {
                    var m = marriages[selectionId - 1];
                    var otherUser = await MikiApp.Instance.Discord.GetUserAsync(m.GetOther(e.Author.Id.ToDbLong()).FromDbLong());

                    await new EmbedBuilder
                    {
                        Title = $"🔔 {e.Locale.GetString("miki_module_accounts_divorce_header")}",
                        Description = e.Locale.GetString("miki_module_accounts_divorce_content", e.Author.Username, otherUser.Username),
                        Color = new Color(0.6f, 0.4f, 0.1f)
                    }.ToEmbed().QueueToChannelAsync(e.Channel);

                    m.Remove(context);
                    await context.SaveChangesAsync();
                }
                else
                {
                    var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));

                    var embed = new EmbedBuilder()
                    {
                        Title = "💍 Marriages",
                        Footer = new EmbedFooter()
                        {
                            Text = $"Use {await e.Prefix.GetForGuildAsync(context, cache, e.Guild.Id)}divorce <number> to decline",
                        },
                        Color = new Color(154, 170, 180)
                    };

                    await BuildMarriageEmbedAsync(embed, e.Author.Id.ToDbLong(), context, marriages);
                    await embed.ToEmbed().QueueToChannelAsync(e.Channel);
                }
            }
        }

        [Command(Name = "marry")]
        public async Task MarryAsync(EventContext e)
        {
            long askerId = 0;
            long receiverId = 0;



            if (!e.Arguments.Take(out string args))
            {
                return;
            }

            IDiscordGuildUser user = await DiscordExtensions.GetUserAsync(args, e.Guild);

            if (user == null)
            {
                e.Channel.QueueMessage("Couldn't find this person..");
                return;
            }

            if (user.Id == (await e.Guild.GetSelfAsync()).Id)
            {
                e.Channel.QueueMessage("(´・ω・`)");
                return;
            }

            using (MikiContext context = new MikiContext())
            {
                MarriageRepository repository = new MarriageRepository(context);

                User mentionedPerson = await User.GetAsync(context, user.Id.ToDbLong(), user.Username);
                User currentUser = await DatabaseHelpers.GetUserAsync(context, e.Author);

                askerId = currentUser.Id;
                receiverId = mentionedPerson.Id;

                if (currentUser == null || mentionedPerson == null)
                {
                    await e.ErrorEmbed(e.Locale.GetString("miki_module_accounts_marry_error_null")).ToEmbed().QueueToChannelAsync(e.Channel);
                    return;
                }

                if (mentionedPerson.Banned)
                {
                    await e.ErrorEmbed("This person has been banned from Miki.").ToEmbed().QueueToChannelAsync(e.Channel);
                    return;
                }

                if (mentionedPerson.Id == currentUser.Id)
                {
                    await e.ErrorEmbed(e.Locale.GetString("miki_module_accounts_marry_error_null")).ToEmbed().QueueToChannelAsync(e.Channel);
                    return;
                }

                if (await repository.ExistsAsync(mentionedPerson.Id, currentUser.Id))
                {
                    await e.ErrorEmbed(e.Locale.GetString("miki_module_accounts_marry_error_exists")).ToEmbed().QueueToChannelAsync(e.Channel);
                    return;
                }

                await repository.ProposeAsync(askerId, receiverId);

                await context.SaveChangesAsync();
            }

            await new EmbedBuilder()
                .SetTitle("💍" + e.Locale.GetString("miki_module_accounts_marry_text", $"**{e.Author.Username}**", $"**{user.Username}**"))
                .SetDescription(e.Locale.GetString("miki_module_accounts_marry_text2", user.Username, e.Author.Username))
                .SetColor(0.4f, 0.4f, 0.8f)
                .SetThumbnail("https://i.imgur.com/TKZSKIp.png")
                .AddInlineField("✅ To accept", $">acceptmarriage @user")
                .AddInlineField("❌ To decline", $">declinemarriage @user")
                .SetFooter("Take your time though! This proposal won't disappear", "")
                .ToEmbed().QueueToChannelAsync(e.Channel);
        }

        [Command(Name = "showproposals")]
		public async Task ShowProposalsAsync(EventContext e)
		{
            if(e.Arguments.Take(out int page))
            {
                page = page - 1;
            }

			using (var context = new MikiContext())
			{
				MarriageRepository repository = new MarriageRepository(context);

				List<UserMarriedTo> proposals = await repository.GetProposalsReceived(e.Author.Id.ToDbLong());
				List<string> proposalNames = new List<string>();

				foreach (UserMarriedTo p in proposals)
				{
					long id = p.GetOther(e.Author.Id.ToDbLong());
					string u = (await MikiApp.Instance.Discord.GetUserAsync(id.FromDbLong())).Username;
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

				proposals = await repository.GetProposalsSent(e.Author.Id.ToDbLong());
				proposalNames = new List<string>();

				foreach (UserMarriedTo p in proposals)
				{
					long id = p.GetOther(e.Author.Id.ToDbLong());
					string u = (await MikiApp.Instance.Discord.GetUserAsync(id.FromDbLong())).Username;
					proposalNames.Add($"{u} [{id}]");
				}

				pageCount = Math.Max(pageCount, (int)Math.Ceiling((float)proposalNames.Count / 35));

				proposalNames = proposalNames.Skip(page * 35)
					.Take(35)
					.ToList();

				output = string.Join("\n", proposalNames);

				embed.AddField("Proposals Sent", string.IsNullOrEmpty(output) ? "none (yet!)" : output);

				embed.Color = new Color(1, 0.5f, 0);
				embed.ThumbnailUrl = (await e.Guild.GetMemberAsync(e.Author.Id)).GetAvatarUrl();
				if (pageCount > 1)
				{
					embed.SetFooter(e.Locale.GetString("page_footer", page + 1, pageCount));
				}
				await embed.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		private async Task<EmbedBuilder> BuildMarriageEmbedAsync(EmbedBuilder embed, long userId, MikiContext context, List<UserMarriedTo> marriages)
		{
			StringBuilder builder = new StringBuilder();

			for (int i = 0; i < marriages.Count; i++)
			{
				builder.AppendLine($"`{(i + 1).ToString().PadLeft(2)}:` {(await MikiApp.Instance.Discord.GetUserAsync(marriages[i].GetOther(userId).FromDbLong())).Username}");
			}

			embed.Description += "\n\n" + builder.ToString();

			return embed;
		}
	}
}