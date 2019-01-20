using Microsoft.EntityFrameworkCore;
using Miki.Configuration;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Exceptions;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miki.Modules
{
	[Module("pasta")]
	public class PastaModule
	{
		[Configurable]
		public ulong PastaReportsChannelId { get; set; } = 0;

		[Command(Name = "mypasta")]
		public async Task MyPasta(EventContext e)
		{
            if(e.Arguments.Take(out int page))
            {
                page--;
            }

			long userId;
			string userName;
			if (e.message.MentionedUserIds.Count() > 0)
			{
				userId = e.message.MentionedUserIds.First().ToDbLong();
				userName = (await e.Guild.GetMemberAsync(userId.FromDbLong())).Username;
			}
			else
			{
				userId = e.Author.Id.ToDbLong();
				userName = e.Author.Username;
			}

			using (var context = new MikiContext())
			{
				var pastasFound = await context.Pastas.Where(x => x.CreatorId == userId)
					.OrderByDescending(x => x.Id)
					.Skip(page * 25)
					.Take(25)
					.ToListAsync();

				var totalCount = await context.Pastas.Where(x => x.CreatorId == userId)
					.CountAsync();

				if (page * 25 > totalCount)
				{
                    await e.ErrorEmbed(e.Locale.GetString("pasta_error_out_of_index"))
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

				if (pastasFound?.Count > 0)
				{
					string resultString = "";

					pastasFound.ForEach(x => { resultString += "`" + x.Id + "` "; });

					await new EmbedBuilder()
						.SetTitle(e.Locale.GetString("mypasta_title", userName))
						.SetDescription(resultString)
						.SetFooter(e.Locale.GetString("page_index", page + 1, (Math.Ceiling((double)totalCount / 25)).ToString()), null)
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

				await e.ErrorEmbed(e.Locale.GetString("mypasta_error_no_pastas"))
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "createpasta")]
		public async Task CreatePasta(EventContext e)
		{
			if (e.Arguments.Pack.Length < 2)
			{
				await e.ErrorEmbed(e.Locale.GetString("createpasta_error_no_content"))
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

            e.Arguments.Take(out string id);
            string text = e.Arguments.Pack.TakeAll();

			if (Regex.IsMatch(text, "(http[s]://)?((discord.gg)|(discordapp.com/invite))/([A-Za-z0-9]+)", RegexOptions.IgnoreCase))
			{
				throw new PastaInviteException();
			}

			using (var context = new MikiContext())
			{
				await GlobalPasta.AddAsync(context, id, text, (long)e.Author.Id);
				await context.SaveChangesAsync();
			}

			await e.SuccessEmbed(e.Locale.GetString("miki_module_pasta_create_success", id))
				.QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "deletepasta")]
		public async Task DeletePasta(EventContext e)
		{
            string pastaArg = e.Arguments.Pack.TakeAll();

            if (string.IsNullOrWhiteSpace(pastaArg))
			{
				await e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_error_specify", e.Locale.GetString("miki_module_pasta_error_specify")))
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

			using (var context = new MikiContext())
			{
				GlobalPasta pasta = await context.Pastas.FindAsync(pastaArg);

				if (pasta == null)
				{
					await e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_error_null")).ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

				if (pasta.CreatorId == e.Author.Id.ToDbLong())
				{
					context.Pastas.Remove(pasta);

					List<PastaVote> votes = context.Votes.Where(p => p.Id == pastaArg).ToList();
					context.Votes.RemoveRange(votes);

					await context.SaveChangesAsync();

					await e.SuccessEmbed(e.Locale.GetString("miki_module_pasta_delete_success", pastaArg)).QueueToChannelAsync(e.Channel);
					return;
				}
				await e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_error_no_permissions", e.Locale.GetString("miki_module_pasta_error_specify_delete")))
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}
		}

		[Command(Name = "editpasta")]
		public async Task EditPasta(EventContext e)
		{
			if (e.Arguments.Pack.Length < 2)
			{
				await e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_error_specify", e.Locale.GetString("miki_module_pasta_error_specify_edit")))
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

			using (var context = new MikiContext())
			{
                e.Arguments.Take(out string tag);

				GlobalPasta p = await context.Pastas.FindAsync(tag);

				if (p.CreatorId == e.Author.Id.ToDbLong())
				{
                    p.Text = e.Arguments.Pack.TakeAll();
					await context.SaveChangesAsync();
                    await e.SuccessEmbed($"Edited `{tag}`!")
                        .QueueToChannelAsync(e.Channel);
                }
				else
				{
                    await e.ErrorEmbed($"You cannot edit pastas you did not create. Baka!")
                        .ToEmbed().QueueToChannelAsync(e.Channel);
				}
			}
		}

		[Command(Name = "pasta")]
		public async Task GetPasta(EventContext e)
		{
            string pastaArg = e.Arguments.Pack.TakeAll();
			if (string.IsNullOrWhiteSpace(pastaArg))
			{
                await e.ErrorEmbed(e.Locale.GetString("pasta_error_no_arg")).ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

			using (var context = new MikiContext())
			{
				GlobalPasta pasta = await context.Pastas.FindAsync(pastaArg);
				if (pasta == null)
				{
                    await e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_search_error_no_results", pastaArg))
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}
				pasta.TimesUsed++;
				
				var sanitizedText = Utils.EscapeEveryone(pasta.Text);
				e.Channel.QueueMessage(sanitizedText);
				await context.SaveChangesAsync();
			}
		}

		[Command(Name = "infopasta")]
		public async Task IdentifyPasta(EventContext e)
		{
            string pastaArg = e.Arguments.Pack.TakeAll();
            if (string.IsNullOrWhiteSpace(pastaArg))
			{
                await e.ErrorEmbed(e.Locale.GetString("infopasta_error_no_arg"))
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

			using (var context = new MikiContext())
			{
				GlobalPasta pasta = await context.Pastas.FindAsync(pastaArg);

				if (pasta == null)
				{
					await e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_error_null")).ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

				User creator = await context.Users.FindAsync(pasta.CreatorId);

				EmbedBuilder b = new EmbedBuilder();

				b.SetAuthor(pasta.Id.ToUpper(), "", "");
				b.Color = new Color(47, 208, 192);

				if (creator != null)
				{
					b.AddInlineField(e.Locale.GetString("miki_module_pasta_identify_created_by"), $"{ creator.Name} [{creator.Id}]");
				}

				b.AddInlineField(e.Locale.GetString("miki_module_pasta_identify_date_created"), pasta.CreatedAt.ToShortDateString());

				b.AddInlineField(e.Locale.GetString("miki_module_pasta_identify_times_used"), pasta.TimesUsed.ToString());

				VoteCount v = await pasta.GetVotesAsync(context);

				b.AddInlineField(e.Locale.GetString("infopasta_rating"), $"⬆️ { v.Upvotes} ⬇️ {v.Downvotes}");

				await b.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "searchpasta")]
		public async Task SearchPasta(EventContext e)
		{
			if (!e.Arguments.Take(out string query))
			{
				await e.ErrorEmbed(e.Locale.GetString("searchpasta_error_no_arg"))
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}
            e.Arguments.Take(out int page);

			using (var context = new MikiContext())
			{
				var pastasFound = await context.Pastas.Where(x => x.Id.ToLower().Contains(query.ToLower()))
					.OrderByDescending(x => x.Id)
					.Skip(25 * page)
					.Take(25)
					.ToListAsync();

				var totalCount = await context.Pastas.Where(x => x.Id.Contains(query))
					.CountAsync();

				if (pastasFound?.Count > 0)
				{
					string resultString = "";

					pastasFound.ForEach(x => { resultString += "`" + x.Id + "` "; });

					await new EmbedBuilder
					{
						Title = e.Locale.GetString("miki_module_pasta_search_header"),
						Description = resultString
					}.SetFooter(e.Locale.GetString("page_index", page + 1, (Math.Ceiling((double)totalCount / 25)).ToString()))
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

                await e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_search_error_no_results", query))
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "lovedpasta", Aliases = new string[] { "lovedpastas", "favouritepastas", "lovepastalist" })]
		public async Task LovePastaList(EventContext e)
		{
			await FavouritePastaList(e);
		}

		[Command(Name = "hatedpasta", Aliases = new string[] { "hatedpastas", "hatepastalist" })]
		public async Task HatePastaList(EventContext e)
		{
			await FavouritePastaList(e, false);
		}

		public async Task FavouritePastaList(EventContext e, bool lovedPastas = true)
		{
			IDiscordUser targetUser = e.Author;
			float totalPerPage = 25f;

            e.Arguments.Take(out int page);

			using (MikiContext context = new MikiContext())
			{
				long authorId = targetUser.Id.ToDbLong();
				List<PastaVote> pastaVotes = await context.Votes.Where(x => x.UserId == authorId && x.PositiveVote == lovedPastas).ToListAsync();

				int maxPage = (int)Math.Floor(pastaVotes.Count() / totalPerPage);
				page = page > maxPage ? maxPage : page;
				page = page < 0 ? 0 : page;

				if (pastaVotes.Count() <= 0)
				{
					string loveString = (lovedPastas ? e.Locale.GetString("miki_module_pasta_loved") : e.Locale.GetString("miki_module_pasta_hated"));
					string errorString = e.Locale.GetString("miki_module_pasta_favlist_self_none", loveString);
					if (e.message.MentionedUserIds.Count() >= 1)
					{
						errorString = e.Locale.GetString("miki_module_pasta_favlist_mention_none", loveString);
					}
					await Utils.ErrorEmbed(e, errorString).ToEmbed()
                        .QueueToChannelAsync(e.Channel);
					return;
				}

				EmbedBuilder embed = new EmbedBuilder();
				List<PastaVote> neededPastas = pastaVotes.Skip((int)totalPerPage * page).Take((int)totalPerPage).ToList();

				string resultString = string.Join(" ", neededPastas.Select(x => $"`{x.Id}`"));

				string useName = string.IsNullOrEmpty(targetUser.Username) ? targetUser.Username : targetUser.Username;
				embed.SetTitle($"{(lovedPastas ? e.Locale.GetString("miki_module_pasta_loved_header") : e.Locale.GetString("miki_module_pasta_hated_header"))} - {useName}");
				embed.SetDescription(resultString);
				embed.SetFooter(e.Locale.GetString("page_index", page + 1, Math.Ceiling(pastaVotes.Count() / totalPerPage)), "");

				await embed.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "lovepasta")]
		public async Task LovePasta(EventContext e)
		{
			await VotePasta(e, true);
		}

		[Command(Name = "hatepasta")]
		public async Task HatePasta(EventContext e)
		{
			await VotePasta(e, false);
		}

        private async Task VotePasta(EventContext e, bool vote)
        {
            if (e.Arguments.Take(out string pastaName))
            {
                using (var context = new MikiContext())
                {
                    var pasta = await context.Pastas.FindAsync(pastaName);

                    if (pasta == null)
                    {
                        await e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_error_null")).ToEmbed().QueueToChannelAsync(e.Channel);
                        return;
                    }

                    long authorId = e.Author.Id.ToDbLong();

                    var voteObject = context.Votes
                        .Where(q => q.Id == pastaName && q.UserId == authorId)
                        .FirstOrDefault();

                    if (voteObject == null)
                    {
                        voteObject = new PastaVote()
                        {
                            Id = pastaName,
                            UserId = e.Author.Id.ToDbLong(),
                            PositiveVote = vote
                        };

                        context.Votes.Add(voteObject);
                    }
                    else
                    {
                        voteObject.PositiveVote = vote;
                    }

                    await context.SaveChangesAsync();

                    var votecount = await pasta.GetVotesAsync(context);
                    pasta.Score = votecount.Upvotes - votecount.Downvotes;

                    await context.SaveChangesAsync();

                    await e.SuccessEmbed(e.Locale.GetString("miki_module_pasta_vote_success", votecount.Upvotes - votecount.Downvotes)).QueueToChannelAsync(e.Channel);
                }
            }
        }
	}
}