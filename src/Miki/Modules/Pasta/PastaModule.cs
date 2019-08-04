namespace Miki.Modules.Pasta
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Miki.Bot.Models;
    using Miki.Configuration;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Rest;
    using Miki.Exceptions;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Attributes;

    [Module("pasta")]
	public class PastaModule
	{
		[Configurable]
		public ulong PastaReportsChannelId { get; set; } = 0;

		[Command("mypasta")]
		public async Task MyPastaAsync(IContext e)
		{
			if(e.GetArgumentPack().Take(out int page))
			{
				page--;
			}

			long userId;
			string userName;
			if(e.GetMessage().MentionedUserIds.Any())
			{
				userId = e.GetMessage().MentionedUserIds.First().ToDbLong();
                userName = (await e.GetGuild().GetMemberAsync((ulong)userId)).Username;
			}
			else
			{
				userId = e.GetAuthor().Id.ToDbLong();
				userName = e.GetAuthor().Username;
			}

			var context = e.GetService<MikiDbContext>();

			var pastasFound = await context.Pastas
                .Where(x => x.CreatorId == userId)
				.OrderByDescending(x => x.Id)
				.Skip(page * 25)
				.Take(25)
				.ToListAsync()
                .ConfigureAwait(false);

			var totalCount = await context.Pastas
                .Where(x => x.CreatorId == userId)
				.CountAsync()
                .ConfigureAwait(false);

			if(page * 25 > totalCount)
			{
				await e.ErrorEmbed(
                        e.GetLocale().GetString("pasta_error_out_of_index"))
					.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}

			if(pastasFound?.Count > 0)
			{
				var resultString = string.Empty;

				pastasFound.ForEach(x => { resultString += "`" + x.Id + "` "; });

				await new EmbedBuilder()
					.SetTitle(e.GetLocale().GetString("mypasta_title", userName))
					.SetDescription(resultString)
					.SetFooter(
                        e.GetLocale().GetString(
                            "page_index", 
                            page + 1, 
                            Math.Ceiling((double)totalCount / 25)).ToString(), null)
					.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}

            await e.ErrorEmbed(e.GetLocale().GetString("mypasta_error_no_pastas"))
                .ToEmbed()
                .QueueAsync(e.GetChannel())
                .ConfigureAwait(false);
        }

		[Command("createpasta")]
		public async Task CreatePastaAsync(IContext e)
        {
			if(e.GetArgumentPack().Pack.Length < 2)
			{
				await e.ErrorEmbed(e.GetLocale().GetString("createpasta_error_no_content"))
					.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}

			e.GetArgumentPack().Take(out string id);

            string text = e.GetArgumentPack()
                .Pack.TakeAll();

			if(Regex.IsMatch(
                text, 
                "(http[s]://)?((discord.gg)|(discordapp.com/invite))/([A-Za-z0-9]+)", 
                RegexOptions.IgnoreCase))
			{
				throw new PastaInviteException();
			}

			var context = e.GetService<MikiDbContext>();

			await GlobalPasta.AddAsync(context, id, text, (long)e.GetAuthor().Id)
                .ConfigureAwait(false);

			await context.SaveChangesAsync()
                .ConfigureAwait(false);

            await e.SuccessEmbedResource("miki_module_pasta_create_success", id)
                .QueueAsync(e.GetChannel())
                .ConfigureAwait(false);
        }

		[Command("deletepasta")]
		public async Task DeletePastaAsync(IContext e)
        {
            var locale = e.GetLocale();

			string pastaArg = e.GetArgumentPack()
                .Pack.TakeAll();

			if(string.IsNullOrWhiteSpace(pastaArg))
			{
				await e.ErrorEmbed(
                        locale.GetString("miki_module_pasta_error_specify",
                            locale.GetString("miki_module_pasta_error_specify")))
					.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}

			var context = e.GetService<MikiDbContext>();

			GlobalPasta pasta = await context.Pastas
                .FindAsync(pastaArg)
                .ConfigureAwait(false);

			if(pasta == null)
			{
				await e.ErrorEmbedResource("miki_module_pasta_error_null")
                    .ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}

			if(pasta.CreatorId == e.GetAuthor().Id.ToDbLong())
			{
				context.Pastas.Remove(pasta);

				List<PastaVote> votes = context.Votes
                    .Where(p => p.Id == pastaArg)
                    .ToList();
				context.Votes.RemoveRange(votes);

				await context.SaveChangesAsync()
                    .ConfigureAwait(false);

				await e.SuccessEmbedResource("miki_module_pasta_delete_success", pastaArg)
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}
			await e.ErrorEmbedResource(
                    "miki_module_pasta_error_no_permissions", 
                    e.GetLocale().GetString("miki_module_pasta_error_specify_delete"))
				.ToEmbed()
                .QueueAsync(e.GetChannel())
                .ConfigureAwait(false);
		}

		[Command("editpasta")]
		public async Task EditPastaAsync(IContext e)
		{
			if(e.GetArgumentPack().Pack.Length < 2)
			{
				await e.ErrorEmbedResource(
                        "miki_module_pasta_error_specify", 
                        e.GetLocale().GetString("miki_module_pasta_error_specify_edit"))
					.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}

			var context = e.GetService<MikiDbContext>();

			e.GetArgumentPack().Take(out string tag);

			GlobalPasta p = await context.Pastas.FindAsync(tag)
                .ConfigureAwait(false);

            if(p.CreatorId == e.GetAuthor().Id.ToDbLong())
			{
				p.Text = e.GetArgumentPack().Pack.TakeAll();

                await context.SaveChangesAsync()
                    .ConfigureAwait(false);

                await e.SuccessEmbed($"Edited `{tag}`!")
					.QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
            }
			else
			{
				await e.ErrorEmbed("You cannot edit pastas you did not create. Baka!")
					.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
			}
		}

		[Command("pasta")]
		public async Task GetPastaAsync(IContext e)
		{
			string pastaArg = e.GetArgumentPack().Pack.TakeAll();
			if(string.IsNullOrWhiteSpace(pastaArg))
			{
				await e.ErrorEmbedResource("pasta_error_no_arg")
                    .ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
                return;
			}

			var context = e.GetService<MikiDbContext>();

			GlobalPasta pasta = await context.Pastas.FindAsync(pastaArg)
                .ConfigureAwait(false);

            if(pasta == null)
			{
				await e.ErrorEmbedResource(
                        "miki_module_pasta_search_error_no_results", 
                        pastaArg)
					.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
                return;
			}
			pasta.TimesUsed++;

			var sanitizedText = Utils.EscapeEveryone(pasta.Text);

            e.GetChannel().QueueMessage(sanitizedText);

            await context.SaveChangesAsync()
                .ConfigureAwait(false);
        }

		[Command("infopasta")]
		public async Task IdentifyPastaAsync(IContext e)
		{
			string pastaArg = e.GetArgumentPack().Pack.TakeAll();
			if(string.IsNullOrWhiteSpace(pastaArg))
			{
				await e.ErrorEmbedResource("infopasta_error_no_arg")
					.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
                return;
			}

			var context = e.GetService<MikiDbContext>();

			GlobalPasta pasta = await context.Pastas.FindAsync(pastaArg)
                .ConfigureAwait(false);

            if(pasta == null)
			{
				await e.ErrorEmbedResource("miki_module_pasta_error_null")
                    .ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
                return;
			}

			User creator = await context.Users.FindAsync(pasta.CreatorId)
                .ConfigureAwait(false);

            EmbedBuilder b = new EmbedBuilder();

			b.SetAuthor(pasta.Id.ToUpperInvariant());
			b.Color = new Color(47, 208, 192);

			if(creator != null)
			{
				b.AddInlineField(
                    e.GetLocale().GetString("miki_module_pasta_identify_created_by"), 
                    $"{ creator.Name} [{creator.Id}]");
			}

			b.AddInlineField(
                e.GetLocale().GetString("miki_module_pasta_identify_date_created"), 
                pasta.CreatedAt.ToShortDateString());

			b.AddInlineField(
                e.GetLocale().GetString("miki_module_pasta_identify_times_used"), 
                pasta.TimesUsed.ToString());

			var v = await pasta.GetVotesAsync(context)
                .ConfigureAwait(false);

            b.AddInlineField(
                e.GetLocale().GetString("infopasta_rating"), 
                $"⬆️ { v.Upvotes} ⬇️ {v.Downvotes}");

			await b.ToEmbed()
                .QueueAsync(e.GetChannel())
                .ConfigureAwait(false);
        }

		[Command("searchpasta")]
		public async Task SearchPastaAsync(IContext e)
		{
			if(!e.GetArgumentPack().Take(out string query))
			{
				await e.ErrorEmbed(e.GetLocale().GetString("searchpasta_error_no_arg"))
					.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
                return;
			}
			e.GetArgumentPack().Take(out int page);

			var context = e.GetService<MikiDbContext>();

			var pastasFound = await context.Pastas
                .Where(x => x.Id.ToLower().Contains(query.ToLower()))
				.OrderByDescending(x => x.Id)
				.Skip(25 * page)
				.Take(25)
				.ToListAsync()
                .ConfigureAwait(false);

            var totalCount = await context.Pastas
                .Where(x => x.Id.Contains(query))
				.CountAsync()
                .ConfigureAwait(false);

            if(pastasFound?.Count > 0)
			{
				string resultString = "";

				pastasFound.ForEach(x => { resultString += "`" + x.Id + "` "; });

				await new EmbedBuilder
				{
					Title = e.GetLocale().GetString("miki_module_pasta_search_header"),
					Description = resultString
				}.SetFooter(
                        e.GetLocale().GetString(
                            "page_index", 
                            page + 1, 
                            Math.Ceiling((double)totalCount / 25)))
					.ToEmbed()
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
                return;
			}

			await e.ErrorEmbed(
                    e.GetLocale().GetString("miki_module_pasta_search_error_no_results", query))
				.ToEmbed()
                .QueueAsync(e.GetChannel())
                .ConfigureAwait(false);
        }

		[Command("lovedpasta", "lovedpastas", "favouritepastas")]
		public Task LovePastaListAsync(IContext e)
		{
			return FavouritePastaListAsync(e);
		}

		[Command("hatedpasta", "hatedpastas")]
		public Task HatePastaListAsync(IContext e)
		{
			return FavouritePastaListAsync(e, false);
		}

		public async Task FavouritePastaListAsync(IContext e, bool lovedPastas = true)
		{
			IDiscordUser targetUser = e.GetAuthor();
			const float totalPerPage = 25f;

			e.GetArgumentPack().Take(out int page);

            var locale = e.GetLocale();
			var context = e.GetService<MikiDbContext>();

			long authorId = targetUser.Id.ToDbLong();
			List<PastaVote> pastaVotes = await context.Votes
                .Where(x => x.UserId == authorId && x.PositiveVote == lovedPastas)
                .ToListAsync()
                .ConfigureAwait(false);

			int maxPage = (int)Math.Floor(pastaVotes.Count() / totalPerPage);
			page = page > maxPage ? maxPage : page;
			page = page < 0 ? 0 : page;

            // TODO(velddev): Turn this all into a exception.
			if(!pastaVotes.Any())
            {
                string loveString = lovedPastas
                    ? locale.GetString("miki_module_pasta_loved")
                    : locale.GetString("miki_module_pasta_hated");

				string errorString = locale.GetString(
                    "miki_module_pasta_favlist_self_none", 
                    loveString);

                if(e.GetMessage().MentionedUserIds.Any())
				{
					errorString = e.GetLocale().GetString(
                        "miki_module_pasta_favlist_mention_none", 
                        loveString);
				}

				await e.ErrorEmbed(errorString)
                    .ToEmbed()
					.QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
				return;
			}

			EmbedBuilder embed = new EmbedBuilder();
            List<PastaVote> neededPastas = pastaVotes.Skip((int)totalPerPage * page)
                .Take((int)totalPerPage)
                .ToList();
            
            string resultString = string.Join(" ", neededPastas.Select(x => $"`{x.Id}`"));

			embed.SetTitle(
                $"{locale.GetString(lovedPastas ? "miki_module_pasta_loved_header" : "miki_module_pasta_hated_header")} - {targetUser.Username}");
			embed.SetDescription(resultString);
			embed.SetFooter(
				locale.GetString(
                    "page_index", 
                    page + 1, 
                    Math.Ceiling(pastaVotes.Count() / totalPerPage)));

			await embed.ToEmbed()
                .QueueAsync(e.GetChannel())
                .ConfigureAwait(false);
		}

		[Command("lovepasta")]
		public Task LovePastaAsync(IContext e)
		{
            return VotePastaAsync(e, true);
		}

		[Command("hatepasta")]
		public Task HatePastaAsync(IContext e)
		{
            return VotePastaAsync(e, false);
		}

		private async Task VotePastaAsync(IContext e, bool vote)
		{
			if(e.GetArgumentPack().Take(out string pastaName))
			{
				var context = e.GetService<MikiDbContext>();

				var pasta = await context.Pastas.FindAsync(pastaName)
                    .ConfigureAwait(false);

				if(pasta == null)
				{
					await e.ErrorEmbedResource("miki_module_pasta_error_null")
                        .ToEmbed()
                        .QueueAsync(e.GetChannel())
                        .ConfigureAwait(false);
					return;
				}

                long authorId = (long)e.GetAuthor().Id;

				var voteObject = await context.Votes
					.FirstOrDefaultAsync(q => q.Id == pastaName && q.UserId == authorId)
                    .ConfigureAwait(false);

				if(voteObject == null)
				{
					voteObject = new PastaVote()
					{
						Id = pastaName,
						UserId = authorId,
						PositiveVote = vote
					};

					context.Votes.Add(voteObject);
				}
				else
				{
					voteObject.PositiveVote = vote;
				}

				await context.SaveChangesAsync()
                    .ConfigureAwait(false);

				var votecount = await pasta.GetVotesAsync(context)
                    .ConfigureAwait(false);
				pasta.Score = votecount.Upvotes - votecount.Downvotes;

				await context.SaveChangesAsync()
                    .ConfigureAwait(false);

				await e.SuccessEmbedResource("miki_module_pasta_vote_success", pasta.Score)
                    .QueueAsync(e.GetChannel())
                    .ConfigureAwait(false);
			}
		}
	}
}