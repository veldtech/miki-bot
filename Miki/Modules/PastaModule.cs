using Miki.Framework;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Miki.Common.Events;
using Miki.Common.Extensions;
using Miki.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Miki.Languages;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module("pasta")]
    public class PastaModule
    {
        [Command(Name = "mypasta")]
        public async Task MyPasta(EventContext e)
        {
            Locale locale = new Locale(e.Channel.Id);

            int page = 0;
            if (!string.IsNullOrWhiteSpace(e.arguments))
            {
                List<string> arguments = e.arguments.Split(' ').ToList();
                if (int.TryParse(arguments[0], out page))
                {
                    page -= 1;
                }
            }
            long userId;
            string userName;
            if (e.message.MentionedUserIds.Count() > 0)
            {
                userId = e.message.MentionedUserIds.First().ToDbLong();
                userName = (await e.Guild.GetUserAsync(userId.FromDbLong())).Username;
            }
            else
            {
                userId = e.Author.Id.ToDbLong();
                userName = e.Author.Username;
            }

            using (var context = new MikiContext())
            {
                var pastasFound = context.Pastas.Where(x => x.CreatorId == userId)
                                                .OrderByDescending(x => x.Id)
                                                .Skip(page * 25)
                                                .Take(25)
                                                .ToList();

                var totalCount = context.Pastas.Where(x => x.CreatorId == userId)
                                               .Count();

                if (page * 25 > totalCount)
                {
                    e.ErrorEmbed(e.GetResource("pasta_error_out_of_index"))
                        .QueueToChannel(e.Channel);
                    return;
                }

                if (pastasFound?.Count > 0)
                {
                    string resultString = "";

                    pastasFound.ForEach(x => { resultString += "`" + x.Id + "` "; });

                    Utils.Embed
                        .SetTitle(e.GetResource("mypasta_title", userName))
                        .SetDescription(resultString)
                        .SetFooter(e.GetResource("page_index", page + 1, (Math.Ceiling((double)totalCount / 25)).ToString()), null)
                        .QueueToChannel(e.Channel);
                    return;
                }

                e.ErrorEmbed(e.GetResource("mypasta_error_no_pastas"))
                    .QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "createpasta")]
        public async Task CreatePasta(EventContext e)
        {
            List<string> arguments = e.arguments.Split(' ').ToList();

			Locale locale = new Locale(e.Channel.Id);

			if (arguments.Count < 2)
            {
                e.ErrorEmbed(e.GetResource("createpasta_error_no_content")).QueueToChannel(e.Channel.Id);
                return;
            }

            string id = arguments[0];
            arguments.RemoveAt(0);

            using (var context = new MikiContext())
            {
                GlobalPasta pasta = await context.Pastas.FindAsync(id);

                if (pasta != null)
                {
                    e.ErrorEmbed(e.GetResource("miki_module_pasta_create_error_already_exist")).QueueToChannel(e.Channel);
                    return;
                }

                context.Pastas.Add(new GlobalPasta()
				{
					Id = id,
					Text = e.message.RemoveMentions(string.Join(" ", arguments)),
					CreatorId = e.Author.Id.ToDbLong(),
					CreatedAt = DateTime.Now
				});
                await context.SaveChangesAsync();
                Utils.SuccessEmbed(locale, e.GetResource("miki_module_pasta_create_success", id)).QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "deletepasta")]
        public async Task DeletePasta(EventContext e)
        {
            Locale locale = new Locale(e.Channel.Id);

            if (string.IsNullOrWhiteSpace(e.arguments))
            {
                e.ErrorEmbed(e.GetResource("miki_module_pasta_error_specify", e.GetResource("miki_module_pasta_error_specify")))
                    .QueueToChannel(e.Channel.Id);
                return;
            }

            using (var context = new MikiContext())
            {
                GlobalPasta pasta = await context.Pastas.FindAsync(e.arguments);

                if (pasta == null)
                {
                    e.ErrorEmbed(e.GetResource("miki_module_pasta_error_null")).QueueToChannel(e.Channel);
                    return;
                }

                if (pasta.CanDeletePasta(e.Author.Id))
                {
                    context.Pastas.Remove(pasta);

                    List<PastaVote> votes = context.Votes.Where(p => p.Id == e.arguments).ToList();
                    context.Votes.RemoveRange(votes);

                    await context.SaveChangesAsync();

                    Utils.SuccessEmbed(locale, e.GetResource("miki_module_pasta_delete_success", e.arguments)).QueueToChannel(e.Channel);
                    return;
                }
                e.ErrorEmbed(e.GetResource("miki_module_pasta_error_no_permissions", e.GetResource("miki_module_pasta_error_specify_delete"))).QueueToChannel(e.Channel);
                return;
            }
        }

        [Command(Name = "editpasta")]
        public async Task EditPasta(EventContext e)
        {
			Locale locale = new Locale(e.Channel.Id);

			if (string.IsNullOrWhiteSpace(e.arguments))
            {
                e.ErrorEmbed(e.GetResource("miki_module_pasta_error_specify", e.GetResource("miki_module_pasta_error_specify_edit")))
                    .QueueToChannel(e.Channel.Id);
                return;
            }

            if (e.arguments.Split(' ').Length == 1)
            {
                e.ErrorEmbed(e.GetResource("miki_module_pasta_error_specify", e.GetResource("miki_module_pasta_error_specify_edit")))
                    .QueueToChannel(e.Channel.Id);
                return;
            }

            using (var context = new MikiContext())
            {
                string tag = e.arguments.Split(' ')[0];
                e.arguments = e.arguments.Substring(tag.Length + 1);

                GlobalPasta p = await context.Pastas.FindAsync(tag);

                if (p.CreatorId == e.Author.Id.ToDbLong() || Bot.Instance.Events.Developers.Contains(e.Author.Id))
                {
                    p.Text = e.arguments;
                    await context.SaveChangesAsync();
                    e.Channel.QueueMessageAsync($"Edited `{tag}`!");
                }
                else
                {
                    e.Channel.QueueMessageAsync($@"You cannot edit pastas you did not create. Baka!");
                }
            }
        }

        [Command(Name = "pasta")]
        public async Task GetPasta(EventContext e)
        {
			Locale locale = new Locale(e.Channel.Id);

			if (string.IsNullOrWhiteSpace(e.arguments))
            {
                e.ErrorEmbed(e.GetResource("pasta_error_no_arg")).QueueToChannel(e.Channel);
                return;
            }

            List<string> arguments = e.arguments.Split(' ').ToList();

            using (var context = new MikiContext())
            {

                GlobalPasta pasta = await context.Pastas.FindAsync(arguments[0]);
                if (pasta == null)
                {
                    e.ErrorEmbed(e.GetResource("miki_module_pasta_search_error_no_results", e.arguments)).QueueToChannel(e.Channel);
                    return;
                }
                pasta.TimesUsed++;
                e.Channel.QueueMessageAsync(pasta.Text);
                await context.SaveChangesAsync();
            }
        }

        [Command(Name = "infopasta")]
        public async Task IdentifyPasta(EventContext e)
        {
			Locale locale = new Locale(e.Channel.Id);

			if (string.IsNullOrWhiteSpace(e.arguments))
            {
                e.ErrorEmbed(e.GetResource("infopasta_error_no_arg"))
                    .QueueToChannel(e.Channel.Id);
                return;
            }

            using (var context = new MikiContext())
            {
                GlobalPasta pasta = await context.Pastas.FindAsync(e.arguments);

                if (pasta == null)
                {
                    e.ErrorEmbed(e.GetResource("miki_module_pasta_error_null")).QueueToChannel(e.Channel);
                    return;
                }

                User creator = await context.Users.FindAsync(pasta.CreatorId);

                IDiscordEmbed b = Utils.Embed;

                b.SetAuthor(pasta.Id.ToUpper(), "", "");
                b.Color = new Color(47, 208, 192);

                if (creator != null)
                {
                    b.AddInlineField(e.GetResource("miki_module_pasta_identify_created_by"), $"{ creator.Name} [{creator.Id}]");
                }

                b.AddInlineField(e.GetResource("miki_module_pasta_identify_date_created"), pasta.CreatedAt.ToShortDateString());

                b.AddInlineField(e.GetResource("miki_module_pasta_identify_times_used"), pasta.TimesUsed.ToString());

                VoteCount v = await pasta.GetVotesAsync(context);

                b.AddInlineField(e.GetResource("infopasta_rating"), $"⬆️ { v.Upvotes} ⬇️ {v.Downvotes}");

                b.QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "searchpasta")]
        public async Task SearchPasta(EventContext e)
        {
			Locale locale = new Locale(e.Channel.Id);

			if (string.IsNullOrWhiteSpace(e.arguments))
            {
                e.ErrorEmbed(e.GetResource("searchpasta_error_no_arg"))
                    .QueueToChannel(e.Channel.Id);
                return;
            }

            List<string> arguments = e.arguments.Split(' ').ToList();
            int page = 0;

            if (arguments.Count > 1)
            {
                if (int.TryParse(arguments[arguments.Count - 1], out page))
                {
                    page -= 1;
                }
            }

            string query = arguments[0];

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

                    IDiscordEmbed embed = Utils.Embed;
                    embed.Title = e.GetResource("miki_module_pasta_search_header");
                    embed.Description = resultString;
                    embed.CreateFooter();
                    embed.Footer.Text = e.GetResource("page_index", page + 1, (Math.Ceiling((double)totalCount / 25)).ToString());

                    embed.QueueToChannel(e.Channel);
                    return;
                }

                e.ErrorEmbed(e.GetResource("miki_module_pasta_search_error_no_results", arguments[0]))
                    .QueueToChannel(e.Channel);
            }
        }

		[Command(Name = "lovedpasta", Aliases = new string[] { "lovedpastas", "favouritepastas", "lovepastalist" } )]
		public async Task LovePastaList( EventContext e )
		{
			await FavouritePastaList( e );
		}

		[Command( Name = "hatedpasta", Aliases = new string[] { "hatedpastas", "hatepastalist" } )]
		public async Task HatePastaList( EventContext e )
		{
			await FavouritePastaList( e, false );
		}

		public async Task FavouritePastaList( EventContext e, bool lovedPastas = true )
		{
			Locale locale = new Locale(e.Channel.Id);
			IDiscordUser targetUser = e.Author;
			float totalPerPage = 25f;
			int page = 0;

			if( e.message.MentionedUserIds.Count() >= 1 )
			{
				targetUser = await e.Guild.GetUserAsync( e.message.MentionedUserIds.First() );
				string[] args = e.arguments.Split( ' ' );
				int.TryParse( (args.Count() > 1 ? args[1] : "0"), out page );
				page -= page <= 0 ? 0 : 1;
			}
			else
			{
				int.TryParse( e.arguments, out page );
				page -= 1;
			}

			using( MikiContext context = new MikiContext() )
			{
				long authorId = targetUser.Id.ToDbLong();
				IEnumerable<PastaVote> pastaVotes = context.Votes.Where( x => x.UserId == authorId && x.PositiveVote == lovedPastas );
				
				int maxPage = (int)Math.Floor( pastaVotes.Count() / totalPerPage );
				page = page > maxPage ? maxPage : page;
				page = page < 0 ? 0 : page;
				

				if( pastaVotes.Count() <= 0 )
				{
					string loveString = ( lovedPastas ? locale.GetString( "miki_module_pasta_loved" ) : locale.GetString( "miki_module_pasta_hated" ) );
					string errorString = locale.GetString( "miki_module_pasta_favlist_self_none", loveString );
					if( e.message.MentionedUserIds.Count() >= 1 )
					{
						errorString = locale.GetString( "miki_module_pasta_favlist_mention_none", loveString );
					}
					Utils.ErrorEmbed( e, errorString ).QueueToChannel( e.Channel.Id );
					return;
				}

				IDiscordEmbed embed = Utils.Embed;
				List<PastaVote> neededPastas = pastaVotes.Skip( (int)totalPerPage * page ).Take( (int)totalPerPage ).ToList();

				string resultString = "";
				neededPastas.ForEach( x => { resultString += "`" + x.Id + "` "; } );

				string useName = string.IsNullOrEmpty( targetUser.Nickname ) ? targetUser.Username : targetUser.Nickname;
				embed.SetTitle( $"{( lovedPastas ? locale.GetString( "miki_module_pasta_loved_header" ) : locale.GetString( "miki_module_pasta_hated_header" ) )} - {useName}" );
				embed.SetDescription( resultString );
				embed.SetFooter( locale.GetString( "page_index", page + 1, Math.Ceiling( pastaVotes.Count() / totalPerPage ) ), "" );

				embed.QueueToChannel( e.Channel );
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
			Locale locale = e.Channel.GetLocale();

			using (var context = new MikiContext())
			{
				var pasta = await context.Pastas.FindAsync(e.arguments);

				if (pasta == null)
				{
					e.ErrorEmbed(e.GetResource("miki_module_pasta_error_null")).QueueToChannel(e.Channel);
					return;
				}

				long authorId = e.Author.Id.ToDbLong();

				var voteObject = context.Votes.Where(q => q.Id == e.arguments && q.UserId == authorId)
											  .FirstOrDefault();

				if (voteObject == null)
				{
					voteObject = new PastaVote() { Id = e.arguments, UserId = e.Author.Id.ToDbLong(), PositiveVote = vote };
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

				Utils.SuccessEmbed(locale, e.GetResource("miki_module_pasta_vote_success", votecount.Upvotes - votecount.Downvotes)).QueueToChannel(e.Channel);
			}
        }
    }
}
