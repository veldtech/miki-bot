namespace Miki.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Exceptions;
    using Miki.Bot.Models.Repositories;
    using Miki.Cache;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Rest;
    using Miki.Exceptions;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Attributes;
    using Miki.Helpers;
    using Miki.Localization;

    [Module("Marriage")]
	public class MarriageModule
	{
		[Command("buymarriageslot")]
		public async Task BuyMarriageSlotAsync(IContext e)
		{
			var context = e.GetService<MikiDbContext>();

			User user = await DatabaseHelpers.GetUserAsync(context, e.GetAuthor())
                .ConfigureAwait(false);

            int limit = 10;
			bool isDonator = await user.IsDonatorAsync(context)
                .ConfigureAwait(false);

            if(isDonator)
			{
				limit += 5;
			}

			if(user.MarriageSlots >= limit)
			{
				EmbedBuilder embed = e.ErrorEmbed($"For now, **{limit} slots** is the max. sorry :(");

				if(limit == 10 && !isDonator)
				{
					embed.AddField("Pro tip!", "Donators get 5 more slots!")
						.SetFooter("Check `>donate` for more information!");
				}

				embed.Color = new Color(1f, 0.6f, 0.4f);
				await embed.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
                return;
			}

			int costForUpgrade = (user.MarriageSlots - 4) * 2500;

			user.MarriageSlots++;
			user.RemoveCurrency(costForUpgrade);

			await new EmbedBuilder()
			{
				Color = new Color(0.4f, 1f, 0.6f),
				Description = e.GetLocale().GetString("buymarriageslot_success", user.MarriageSlots),
			}.ToEmbed().QueueAsync(e.GetChannel())
                .ConfigureAwait(false);

            await context.SaveChangesAsync()
                .ConfigureAwait(false);
        }

		[Command("acceptmarriage")]
		public async Task AcceptMarriageAsync(IContext e)
		{
			IDiscordUser user = await DiscordExtensions.GetUserAsync(e.GetArgumentPack().Pack.TakeAll(), e.GetGuild());

			if(user == null)
			{
				throw new UserNullException();
			}

			if(user.Id == e.GetAuthor().Id)
            {
                await e.ErrorEmbed("Please mention someone else than yourself.")
                    .ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}

			var context = e.GetService<MikiDbContext>();

			MarriageRepository repository = new MarriageRepository(context);

			User accepter = await DatabaseHelpers.GetUserAsync(context, e.GetAuthor())
                .ConfigureAwait(false);

            User asker = await DatabaseHelpers.GetUserAsync(context, user)
                .ConfigureAwait(false);

            UserMarriedTo marriage = await repository.GetEntryAsync(accepter.Id, asker.Id);

			if(marriage != null)
			{
				if(accepter.MarriageSlots < (await repository.GetMarriagesAsync(accepter.Id)).Count)
				{
					throw new InsufficientMarriageSlotsException(accepter);
				}

				if(asker.MarriageSlots < (await repository.GetMarriagesAsync(asker.Id)).Count)
				{
					throw new InsufficientMarriageSlotsException(asker);
				}

				if(marriage.ReceiverId != e.GetAuthor().Id.ToDbLong())
				{
					e.GetChannel().QueueMessage($"You can not accept your own responses!");
					return;
				}

				if(marriage.Marriage.IsProposing)
				{
					marriage.Marriage.AcceptProposal();

                    await context.SaveChangesAsync()
                        .ConfigureAwait(false);

					await new EmbedBuilder()
					{
						Title = ("❤️ Happily married"),
						Color = new Color(190, 25, 49),
						Description = ($"Much love to { e.GetAuthor().Username } and { user.Username } in their future adventures together!")
					}.ToEmbed().QueueAsync(e.GetChannel())
                        .ConfigureAwait(false);
                }
				else
				{
					await e.ErrorEmbed("You're already married to this person. you doofus!")
						.ToEmbed()
                        .QueueAsync(e.GetChannel())
                        .ConfigureAwait(false);
                }
			}
			else
			{
				await e.ErrorEmbed("This user hasn't proposed to you!")
                    .ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
            }
		}

		[Command("cancelmarriage")]
		public async Task CancelMarriageAsync(IContext e)
		{
			var context = e.GetService<MikiDbContext>();
			MarriageRepository repository = new MarriageRepository(context);

            var marriages = await repository.GetProposalsSent(e.GetAuthor().Id.ToDbLong())
                .ConfigureAwait(false);

			if(!marriages.Any())
			{
				// TODO(velddev): add no propsoals
				//throw new LocalizedException("error_proposals_empty");
				return;
			}

			marriages = marriages.OrderByDescending(x => x.Marriage.TimeOfMarriage).ToList();

			if(e.GetArgumentPack().Take(out int selectionId))
			{
				var m = marriages[selectionId - 1];
                string otherName = (await e.GetService<DiscordClient>()
                    .GetUserAsync(m.GetOther(e.GetAuthor().Id.ToDbLong()).FromDbLong())
                    .ConfigureAwait(false)).Username;

				await new EmbedBuilder
				{
					Title = $"💔 You took back your proposal to {otherName}!",
					Description = $"Aww, don't worry {otherName}. There is plenty of fish in the sea!",
					Color = new Color(231, 90, 112)
				}.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);

				m.Remove(context);
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
			else
			{
				var embed = new EmbedBuilder()
				{
					Title = "💍 Proposals",
					Footer = new EmbedFooter()
					{
						Text = $"Use {e.GetPrefixMatch()}cancelmarriage <number> to decline",
					},
					Color = new Color(154, 170, 180)
				};

                await this.BuildMarriageEmbedAsync(embed, e.GetAuthor().Id.ToDbLong(), marriages)
                    .ConfigureAwait(false);

                await embed.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
            }
		}

		[Command("declinemarriage")]
		public async Task DeclineMarriageAsync(IContext e)
		{
			var context = e.GetService<MikiDbContext>();

			MarriageRepository repository = new MarriageRepository(context);

			var marriages = await repository.GetProposalsReceived(e.GetAuthor().Id.ToDbLong());

			if(marriages.Count == 0)
			{
				// TODO: add no propsoals
				//throw new LocalizedException("error_proposals_empty");
				return;
			}

			marriages = marriages.OrderByDescending(x => x.Marriage.TimeOfMarriage).ToList();

			if(e.GetArgumentPack().Take(out int selectionId))
			{
				var m = marriages[selectionId - 1];
				string otherName = (await MikiApp.Instance.Services
					.GetService<DiscordClient>()
					.GetUserAsync(m.GetOther(e.GetAuthor().Id.ToDbLong()).FromDbLong())).Username;

				await new EmbedBuilder()
				{
					Title = $"🔫 You shot down {otherName}!",
					Description = $"Aww, don't worry {otherName}. There is plenty of fish in the sea!",
					Color = new Color(191, 105, 82)
				}.ToEmbed().QueueAsync(e.GetChannel());

				m.Remove(context);
				await context.SaveChangesAsync();
			}
			else
			{
				var cache = e.GetService<ICacheClient>();

				var embed = new EmbedBuilder()
				{
					Title = "💍 Proposals",
					Footer = new EmbedFooter()
					{
						Text = $"Use {e.GetPrefixMatch()}declinemarriage <number> to decline",
					},
					Color = new Color(154, 170, 180)
				};

				await this.BuildMarriageEmbedAsync(embed, e.GetAuthor().Id.ToDbLong(), marriages);
				await embed.ToEmbed().QueueAsync(e.GetChannel());
			}
		}

		[Command("divorce")]
		public async Task DivorceAsync(IContext e)
		{
			var context = e.GetService<MikiDbContext>();

			MarriageRepository repository = new MarriageRepository(context);

			var marriages = await repository.GetMarriagesAsync((long)e.GetAuthor().Id);

			if(marriages.Count == 0)
			{
				// TODO: no proposals exception
				return;
			}

			marriages = marriages.OrderByDescending(x => x.Marriage.TimeOfMarriage).ToList();

			if(e.GetArgumentPack().Take(out int selectionId))
			{
				var m = marriages[selectionId - 1];
				var otherUser = await MikiApp.Instance.Services
                    .GetService<DiscordClient>()
					.GetUserAsync(m.GetOther(e.GetAuthor().Id.ToDbLong()).FromDbLong());

				await new EmbedBuilder
				{
					Title = $"🔔 {e.GetLocale().GetString("miki_module_accounts_divorce_header")}",
					Description = e.GetLocale().GetString("miki_module_accounts_divorce_content", e.GetAuthor().Username, otherUser.Username),
					Color = new Color(0.6f, 0.4f, 0.1f)
				}.ToEmbed().QueueAsync(e.GetChannel());

				m.Remove(context);
				await context.SaveChangesAsync();
			}
			else
			{
				var cache = e.GetService<ICacheClient>();

				var embed = new EmbedBuilder()
				{
					Title = "💍 Marriages",
					Footer = new EmbedFooter()
					{
						Text = $"Use {e.GetPrefixMatch()}divorce <number> to decline",
					},
					Color = new Color(154, 170, 180)
				};

                await this.BuildMarriageEmbedAsync(embed, e.GetAuthor().Id.ToDbLong(), marriages)
                    .ConfigureAwait(false);
                await embed.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
            }
		}

		[Command("marry")]
		public async Task MarryAsync(IContext e)
		{
			if(!e.GetArgumentPack().Take(out string args))
			{
				return;
			}

            IDiscordGuildUser user = await DiscordExtensions.GetUserAsync(args, e.GetGuild())
                .ConfigureAwait(false);

			if(user == null)
			{
				e.GetChannel().QueueMessage("Couldn't find this person..");
				return;
			}

			if(user.Id == (await e.GetGuild().GetSelfAsync().ConfigureAwait(false)).Id)
			{
				e.GetChannel().QueueMessage("(´・ω・`)");
				return;
			}

			var context = e.GetService<MikiDbContext>();

			MarriageRepository repository = new MarriageRepository(context);

            User mentionedPerson = await User.GetAsync(context, user.Id.ToDbLong(), user.Username)
                .ConfigureAwait(false);

            User currentUser = await DatabaseHelpers.GetUserAsync(context, e.GetAuthor())
                .ConfigureAwait(false);

			long askerId = currentUser.Id;
			long receiverId = mentionedPerson.Id;

            if(await mentionedPerson.IsBannedAsync(context)
                .ConfigureAwait(false))
            {
                await e.ErrorEmbed("This person has been banned.")
                    .ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}

			if(receiverId == askerId)
            {
                await e.ErrorEmbedResource("miki_module_accounts_marry_error_null")
                    .ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}

			if(await repository.ExistsAsync(receiverId, askerId))
            {
                await e.ErrorEmbedResource("miki_module_accounts_marry_error_exists")
                    .ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}

            await repository.ProposeAsync(askerId, receiverId)
                .ConfigureAwait(false);

            await context.SaveChangesAsync()
                .ConfigureAwait(false);

			await new EmbedBuilder()
				.SetTitle("💍" + e.GetLocale().GetString("miki_module_accounts_marry_text", $"**{e.GetAuthor().Username}**", $"**{user.Username}**"))
				.SetDescription(e.GetLocale().GetString("miki_module_accounts_marry_text2", user.Username, e.GetAuthor().Username))
				.SetColor(0.4f, 0.4f, 0.8f)
				.SetThumbnail("https://i.imgur.com/TKZSKIp.png")
				.AddInlineField("✅ To accept", ">acceptmarriage @user")
				.AddInlineField("❌ To decline", ">declinemarriage @user")
				.SetFooter("Take your time though! This proposal won't disappear", "")
				.ToEmbed()
                .QueueAsync(e.GetChannel())
                .ConfigureAwait(false);
        }

		[Command("showproposals")]
		public async Task ShowProposalsAsync(IContext e)
		{
			if(e.GetArgumentPack().Take(out int page))
			{
				page -= 1;
			}

			var context = e.GetService<MikiDbContext>();

			MarriageRepository repository = new MarriageRepository(context);

			List<UserMarriedTo> proposals = await repository.GetProposalsReceived(e.GetAuthor().Id.ToDbLong());
			List<string> proposalNames = new List<string>();

			foreach(UserMarriedTo p in proposals)
			{
				long id = p.GetOther(e.GetAuthor().Id.ToDbLong());
				string u = (await MikiApp.Instance.Services
                    .GetService<DiscordClient>()
					.GetUserAsync(id.FromDbLong())).Username;
				proposalNames.Add($"{u} [{id}]");
			}

			int pageCount = (int)Math.Ceiling((float)proposalNames.Count / 35);

			proposalNames = proposalNames.Skip(page * 35)
				.Take(35)
				.ToList();

			EmbedBuilder embed = new EmbedBuilder()
				.SetTitle(e.GetAuthor().Username)
				.SetDescription(
                    "Here it shows both the people who you've proposed to and who have proposed to you.");

			string output = string.Join("\n", proposalNames);

			embed.AddField("Proposals Recieved", string.IsNullOrEmpty(output) ? "none (yet!)" : output);

			proposals = await repository.GetProposalsSent(e.GetAuthor().Id.ToDbLong());
			proposalNames = new List<string>();

			foreach(UserMarriedTo p in proposals)
			{
				long id = p.GetOther(e.GetAuthor().Id.ToDbLong());
                string u = (await e.GetService<DiscordClient>()
                    .GetUserAsync(id.FromDbLong()).ConfigureAwait(false)).Username;
				proposalNames.Add($"{u} [{id}]");
			}

			pageCount = Math.Max(pageCount, (int)Math.Ceiling((float)proposalNames.Count / 35));

			proposalNames = proposalNames.Skip(page * 35)
				.Take(35)
				.ToList();

			output = string.Join("\n", proposalNames);

			embed.AddField("Proposals Sent", string.IsNullOrEmpty(output) ? "none (yet!)" : output);

			embed.Color = new Color(1, 0.5f, 0);
            embed.ThumbnailUrl = (await e.GetGuild()
                    .GetMemberAsync(e.GetAuthor().Id)
                    .ConfigureAwait(false))
                .GetAvatarUrl();
			if(pageCount > 1)
			{
				embed.SetFooter(e.GetLocale().GetString("page_footer", page + 1, pageCount));
			}
			await embed.ToEmbed()
                .QueueAsync(e.GetChannel())
                .ConfigureAwait(false);
        }

		private async Task BuildMarriageEmbedAsync(
            EmbedBuilder embed, 
            long userId, 
            IReadOnlyList<UserMarriedTo> marriages)
		{
			StringBuilder builder = new StringBuilder();
			var discord = MikiApp.Instance.Services.GetService<DiscordClient>();

			for(int i = 0; i < marriages.Count; i++)
            {
                var user = await discord.GetUserAsync((ulong)marriages[i].GetOther(userId))
                    .ConfigureAwait(false);

				builder.AppendLine($"`{(i + 1).ToString().PadLeft(2)}:` {user.Username}");
			}

			embed.Description += "\n\n" + builder;
        }
	}
}