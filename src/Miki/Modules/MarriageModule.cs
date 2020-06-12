using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Miki.Bot.Models;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Exceptions;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Localization;
using Miki.Modules.Accounts.Services;
using Miki.Services;
using Miki.Services.Transactions;
using Miki.Utility;
using Miki.Services.Achievements;
using Miki.Services.Marriages;
using Miki.Attributes;

namespace Miki.Modules
{
    [Module("Marriage"), Emoji(AppProps.Emoji.Ring)]
	public class MarriageModule
    {
        [Command("acceptmarriage")]
        public async Task AcceptMarriageAsync(IContext e)
        {
			var userService = e.GetService<IUserService>();

            IDiscordUser user = await e.GetGuild().FindUserAsync(e);

            if(user.Id == e.GetAuthor().Id)
            {
                await e.ErrorEmbed("Please mention someone else than yourself.")
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
                return;
            }

            var service = e.GetService<MarriageService>();

			User accepter = await userService.GetOrCreateUserAsync(e.GetAuthor())
				.ConfigureAwait(false);

			User asker = await userService.GetOrCreateUserAsync(user)
				.ConfigureAwait(false);

            UserMarriedTo marriage = await service.GetEntryAsync(accepter.Id, asker.Id);

            if(marriage != null)
            {
                if(accepter.MarriageSlots < (await service.GetMarriagesAsync(accepter.Id)).Count)
                {
                    throw new InsufficientMarriageSlotsException(accepter);
                }

                if(asker.MarriageSlots < (await service.GetMarriagesAsync(asker.Id)).Count)
                {
                    throw new InsufficientMarriageSlotsException(asker);
                }

                if(marriage.ReceiverId != (long)e.GetAuthor().Id)
                {
                    e.GetChannel().QueueMessage(e, null, $"You can not accept your own responses!");
                    return;
                }

                if(marriage.Marriage.IsProposing)
                {
                    await service.AcceptProposalAsync(marriage.Marriage);

                    await new EmbedBuilder
                    {
                        Title = ("❤️ Happily married"),
                        Color = new Color(190, 25, 49),
                        Description = ($"Much love to { e.GetAuthor().Username } and { user.Username } in their future adventures together!")
                    }.ToEmbed().QueueAsync(e, e.GetChannel())
                        .ConfigureAwait(false);
                }
                else
                {
                    await e.ErrorEmbed("You're already married to this person. you doofus!")
                        .ToEmbed()
                        .QueueAsync(e, e.GetChannel())
                        .ConfigureAwait(false);
                }
            }
            else
            {
                await e.ErrorEmbed("This user hasn't proposed to you!")
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
            }
        }

        [Command("buymarriageslot")]
        public async Task BuyMarriageSlotAsync(IContext e)
        {
            var userService = e.GetService<IUserService>();
            var transactionService = e.GetService<ITransactionService>();
            var locale = e.GetLocale();

            User user = await userService.GetOrCreateUserAsync(e.GetAuthor())
                .ConfigureAwait(false);

            int limit = 10;

            bool isDonator = await userService.UserIsDonatorAsync(user.Id).ConfigureAwait(false);
            if(isDonator)
            {
                limit += 5;
            }

            if(user.MarriageSlots >= limit)
            {
                EmbedBuilder embed = e.ErrorEmbed($"For now, **{limit} slots** is the max. sorry :(");

                if(!isDonator)
                {
                    embed.AddField(
                        locale.GetString("tip_header"),
                        locale.GetString("marriageslot_donator_bonus") +
                        $" [{locale.GetString("marriageslot_footer")}](https://patreon.com/mikibot)");
                }

                await embed.ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
                return;
            }

            int costForUpgrade = user.MarriageSlots * 1500;

            await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithAmount(costForUpgrade)
                        .WithReceiver(AppProps.Currency.BankId)
                        .WithSender(user.Id)
                        .Build())
                .ConfigureAwait(false);

            await userService.AddMarriageSlotAsync(user.Id);
            await e.SuccessEmbedResource("buymarriageslot_success", user.MarriageSlots + 1)
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }

        [Command("cancelmarriage")]
		public async Task CancelMarriageAsync(IContext e)
		{
			var service = e.GetService<MarriageService>();

            var marriages = await service.GetProposalsSentAsync((long)e.GetAuthor().Id)
                .ConfigureAwait(false);

			marriages = marriages.OrderByDescending(x => x.Marriage.TimeOfMarriage).ToList();

			if(e.GetArgumentPack().Take(out int selectionId))
			{
				var m = marriages[selectionId - 1];
                string otherName = (await e.GetService<IDiscordClient>()
					.GetUserAsync(m.GetOther(e.GetAuthor().Id))
                    .ConfigureAwait(false)).Username;

				await service.DeclineProposalAsync(m);

				await new EmbedBuilder
				{
					Title = $"💔 You took back your proposal to {otherName}!",
					Description = $"Aww, don't worry {otherName}. There is plenty of fish in the sea!",
					Color = new Color(231, 90, 112)
				}.ToEmbed()
                    .QueueAsync(e, e.GetChannel())
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

                await this.BuildMarriageEmbedAsync(e, embed, (long)e.GetAuthor().Id, marriages)
                    .ConfigureAwait(false);

                await embed.ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
            }
		}

		[Command("declinemarriage")]
		public async Task DeclineMarriageAsync(IContext e)
		{
			var service = e.GetService<MarriageService>();
            var locale = e.GetLocale();

			var marriages = await service.GetProposalsReceivedAsync((long)e.GetAuthor().Id);

            marriages = marriages.OrderByDescending(x => x.Marriage.TimeOfMarriage).ToList();

			if(e.GetArgumentPack().Take(out int selectionId))
			{
				var m = marriages[selectionId - 1];
				string otherName = (await e.GetService<IDiscordClient>()
					.GetUserAsync(m.GetOther(e.GetAuthor().Id))).Username;
                await service.DeclineProposalAsync(m);

				await new EmbedBuilder()
                    .SetTitle($"🔫 {locale.GetString("marriage_proposal_declined", otherName)}")
                    .SetDescription(locale.GetString("marriage_proposal_declined_text", otherName))
                    .SetColor(191, 105, 82)
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
            }
			else
			{
				var embed = new EmbedBuilder()
				{
					Title = "💍 Proposals",
					Footer = new EmbedFooter()
					{
						Text = $"Use {e.GetPrefixMatch()}declinemarriage <number> to decline",
					},
					Color = new Color(154, 170, 180)
				};
                await this.BuildMarriageEmbedAsync(e, embed, (long)e.GetAuthor().Id, marriages);
				await embed.ToEmbed().QueueAsync(e, e.GetChannel());
			}
		}

		[Command("divorce")]
		public async Task DivorceAsync(IContext e)
		{
            var service = e.GetService<MarriageService>();
            var locale = e.GetLocale();

			var marriages = await service.GetMarriagesAsync((long)e.GetAuthor().Id);
            if(marriages.Count == 0)
			{
				// TODO: no proposals exception
				return;
			}

			marriages = marriages.OrderByDescending(x => x.Marriage.TimeOfMarriage).ToList();

			if(e.GetArgumentPack().Take(out int selectionId))
			{
				var m = marriages[selectionId - 1];
				var otherUser = await e.GetService<IDiscordClient>()
					.GetUserAsync(m.GetOther(e.GetAuthor().Id));
                await service.DeclineProposalAsync(m);

                await new EmbedBuilder()
                    .SetTitle($"🔔 {locale.GetString("miki_module_accounts_divorce_header")}")
                    .SetDescription(locale.GetString("miki_module_accounts_divorce_content",
                        e.GetAuthor().Username, otherUser.Username))
                    .SetColor(0.6f, 0.4f, 0.1f)
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
			}
			else
            {
                var embed = new EmbedBuilder()
                    .SetTitle($"💍 {locale.GetString("entity_marriage").CapitalizeFirst()}")
                    .SetFooter($"Use {e.GetPrefixMatch()}divorce <number> to decline")
                    .SetColor(154, 170, 180);

                await this.BuildMarriageEmbedAsync(e, embed, (long)e.GetAuthor().Id, marriages)
                    .ConfigureAwait(false);
                await embed.ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
            }
		}

		[Command("marry")]
		public async Task MarryAsync(IContext e)
		{
			var userService = e.GetService<IUserService>();
            var locale = e.GetLocale();

            IDiscordGuildUser user = await e.GetGuild().FindUserAsync(e);
            if(user.Id == (await e.GetGuild().GetSelfAsync().ConfigureAwait(false)).Id)
			{
				e.GetChannel().QueueMessage(e, null, "(´・ω・`)");
				return;
			}

            var repository = e.GetService<MarriageService>();

            User mentionedPerson = await userService.GetOrCreateUserAsync(user)
                .ConfigureAwait(false);

            User currentUser = await userService.GetOrCreateUserAsync(e.GetAuthor())
				.ConfigureAwait(false);

			long askerId = currentUser.Id;
			long receiverId = mentionedPerson.Id;

			if(receiverId == askerId)
            {
                var achievements = e.GetService<AchievementService>();
                await achievements.UnlockAsync(e,
                    achievements.GetAchievement(AchievementIds.MarrySelfId),
                    e.GetAuthor().Id);
                return;
			}

			if(await repository.ExistsAsync(receiverId, askerId))
            {
                await e.ErrorEmbedResource("miki_module_accounts_marry_error_exists")
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}

            await repository.ProposeAsync(askerId, receiverId)
                .ConfigureAwait(false);

            await new EmbedBuilder()
				.SetTitle("💍" + locale.GetString(
                              "miki_module_accounts_marry_text", 
                              $"**{e.GetAuthor().Username}**", 
                              $"**{user.Username}**"))
				.SetDescription(locale.GetString(
                    "miki_module_accounts_marry_text2", 
                    user.Username, 
                    e.GetAuthor().Username))
				.SetColor(0.4f, 0.4f, 0.8f)
				.SetThumbnail("https://i.imgur.com/TKZSKIp.png")
				.AddInlineField("✅ To accept", ">acceptmarriage <@user>")
				.AddInlineField("❌ To decline", ">declinemarriage <number>")
				.SetFooter("Take your time though! This proposal won't disappear")
				.ToEmbed()
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }

		[Command("showproposals")]
		public async Task ShowProposalsAsync(IContext e)
		{
			if(e.GetArgumentPack().Take(out int page))
			{
				page -= 1;
			}

            var repository = e.GetService<MarriageService>();

			List<UserMarriedTo> proposals = await repository.GetProposalsReceivedAsync(
                (long)e.GetAuthor().Id);
			List<string> proposalNames = new List<string>();
            var discordclient = e.GetService<IDiscordClient>();

			foreach(UserMarriedTo p in proposals)
			{
				long id = (long)p.GetOther(e.GetAuthor().Id);
				string u = (await discordclient.GetUserAsync(id.FromDbLong())).Username;
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

			proposals = await repository.GetProposalsSentAsync((long)e.GetAuthor().Id);
			proposalNames = new List<string>();

			foreach(UserMarriedTo p in proposals)
			{
				long id = (long)p.GetOther(e.GetAuthor().Id);
                string u = (await discordclient.GetUserAsync(id.FromDbLong())
                    .ConfigureAwait(false)).Username;
				proposalNames.Add($"{u} [{id}]");
			}

			pageCount = Math.Max(pageCount, (int)Math.Ceiling((float)proposalNames.Count / 35));

			proposalNames = proposalNames.Skip(page * 35)
				.Take(35)
				.ToList();

			output = string.Join("\n", proposalNames);

			embed.AddField("Proposals Sent", string.IsNullOrEmpty(output) ? "none (yet!)" : output);

			embed.Color = new Color(1, 0.5f, 0);
            embed.ThumbnailUrl = e.GetAuthor().GetAvatarUrl();

			if(pageCount > 1)
			{
				embed.SetFooter(e.GetLocale().GetString("page_footer", page + 1, pageCount));
			}
			await embed.ToEmbed()
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }

		private async Task BuildMarriageEmbedAsync(
			IContext context, EmbedBuilder embed, long userId, IReadOnlyList<UserMarriedTo> marriages)
		{
			var builder = new StringBuilder();
			var discord = context.GetService<IDiscordClient>();

			for(int i = 0; i < marriages.Count; i++)
            {
                var user = await discord.GetUserAsync((ulong)marriages[i].GetOther(userId))
                    .ConfigureAwait(false);
                builder.AppendLine($"`{i + 1,2}:` {user.Username}");
			}
            embed.Description += "\n\n" + builder;
        }
	}
}
