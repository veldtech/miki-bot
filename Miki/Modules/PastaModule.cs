using Miki.Framework;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Microsoft.EntityFrameworkCore;
using Miki.Languages;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Miki.Framework.Events;
using Miki.Framework.Extension;
using System.Text.RegularExpressions;
using Miki.Framework.Languages;
using Miki.Exceptions;
using Miki.Configuration;
using Miki.Discord;
using Miki.Discord.Rest;
using Miki.Discord.Common;

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
            int page = 0;
            if (!string.IsNullOrWhiteSpace(e.Arguments.ToString()))
            {
                if (int.TryParse(e.Arguments.FirstOrDefault().Argument, out page))
                {
                    page -= 1;
                }
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
                var pastasFound = context.Pastas.Where(x => x.CreatorId == userId)
                                                .OrderByDescending(x => x.Id)
                                                .Skip(page * 25)
                                                .Take(25)
                                                .ToList();

                var totalCount = context.Pastas.Where(x => x.CreatorId == userId)
                                               .Count();

                if (page * 25 > totalCount)
                {
                    e.ErrorEmbed(e.Locale.GetString("pasta_error_out_of_index"))
                        .ToEmbed().QueueToChannel(e.Channel);
                    return;
                }

                if (pastasFound?.Count > 0)
                {
                    string resultString = "";

                    pastasFound.ForEach(x => { resultString += "`" + x.Id + "` "; });

                    Utils.Embed
                        .SetTitle(e.Locale.GetString("mypasta_title", userName))
                        .SetDescription(resultString)
                        .SetFooter(e.Locale.GetString("page_index", page + 1, (Math.Ceiling((double)totalCount / 25)).ToString()), null)
                        .ToEmbed().QueueToChannel(e.Channel);
                    return;
                }

                e.ErrorEmbed(e.Locale.GetString("mypasta_error_no_pastas"))
                    .ToEmbed().QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "createpasta")]
        public async Task CreatePasta(EventContext e)
        {
            if (e.Arguments.Count < 2)
            {
                e.ErrorEmbed(e.Locale.GetString("createpasta_error_no_content"))
                    .ToEmbed().QueueToChannel(e.Channel);
                return;
            }

            ArgObject arg = e.Arguments.FirstOrDefault();

            string id = arg.Argument;
            arg = arg.Next();
            string text = arg.TakeUntilEnd().Argument;

            using (var context = new MikiContext())
            {
                try
                {
                    await GlobalPasta.AddAsync(context, id, text, (long)e.Author.Id);
                    await context.SaveChangesAsync();
                }
                catch (DuplicatePastaException e)
                {
                    e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_create_error_already_exist", id))
                        .QueueToChannel(e.Channel);
                    return;
                }
            }

            e.SuccessEmbed(e.Locale.GetString("miki_module_pasta_create_success", id))
                .ToEmbed().QueueToChannel(e.Channel);
        }

        [Command(Name = "deletepasta")]
        public async Task DeletePasta(EventContext e)
        {
            if (string.IsNullOrWhiteSpace(e.Arguments.ToString()))
            {
                e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_error_specify", e.Locale.GetString("miki_module_pasta_error_specify_delete")))
                    .ToEmbed().QueueToChannel(e.Channel);
                return;
            }

            using (var context = new MikiContext())
            {
                GlobalPasta pasta = await context.Pastas.FindAsync(e.Arguments.ToString());

                if (pasta == null)
                {
                    e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_error_null")).ToEmbed().QueueToChannel(e.Channel);
                    return;
                }

                if (pasta.CreatorId == e.Author.Id.ToDbLong())
                {
                    context.Pastas.Remove(pasta);

                    List<PastaVote> votes = context.Votes.Where(p => p.Id == e.Arguments.ToString()).ToList();
                    context.Votes.RemoveRange(votes);

                    await context.SaveChangesAsync();

                    e.SuccessEmbed(e.Locale.GetString("miki_module_pasta_delete_success", e.Arguments.ToString())).QueueToChannel(e.Channel);
                    return;
                }
                e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_error_no_permissions", e.Locale.GetString("miki_module_pasta_error_specify_delete")))
                    .ToEmbed().QueueToChannel(e.Channel);
                return;
            }
        }

        [Command(Name = "editpasta")]
        public async Task EditPasta(EventContext e)
        {
            if (e.Arguments.Count < 2)
            {
                e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_error_specify", e.Locale.GetString("miki_module_pasta_error_specify_edit")))
                    .ToEmbed().QueueToChannel(e.Channel);
                return;
            }

            using (var context = new MikiContext())
            {
                ArgObject arg = e.Arguments.FirstOrDefault();

                string tag = arg.Argument;
                arg = arg.Next();

                GlobalPasta p = await context.Pastas.FindAsync(tag);

                if (p.CreatorId == e.Author.Id.ToDbLong())
                {
                    p.Text = arg.TakeUntilEnd().Argument;
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
			if (string.IsNullOrWhiteSpace(e.Arguments.ToString()))
            {
                e.ErrorEmbed(e.Locale.GetString("pasta_error_no_arg")).ToEmbed().QueueToChannel(e.Channel);
                return;
            }

            using (var context = new MikiContext())
            {

                GlobalPasta pasta = await context.Pastas.FindAsync(e.Arguments.ToString());
                if (pasta == null)
                {
                    e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_search_error_no_results", e.Arguments.ToString()))
                        .ToEmbed().QueueToChannel(e.Channel);
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
            if (string.IsNullOrWhiteSpace(e.Arguments.ToString()))
            {
                e.ErrorEmbed(e.Locale.GetString("infopasta_error_no_arg"))
                    .ToEmbed().QueueToChannel(e.Channel);
                return;
            }

            using (var context = new MikiContext())
            {
                GlobalPasta pasta = await context.Pastas.FindAsync(e.Arguments.ToString());

                if (pasta == null)
                {
                    e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_error_null")).ToEmbed().QueueToChannel(e.Channel);
                    return;
                }

                User creator = await context.Users.FindAsync(pasta.CreatorId);

                EmbedBuilder b = Utils.Embed;

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

                b.ToEmbed().QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "searchpasta")]
        public async Task SearchPasta(EventContext e)
        {
            ArgObject arg = e.Arguments.FirstOrDefault();

            if(arg == null)
            {
                e.ErrorEmbed(e.Locale.GetString("searchpasta_error_no_arg"))
                    .ToEmbed().QueueToChannel(e.Channel);
                return;
            }

            string query = arg.Argument;

            arg = arg.Next();

            int page = (arg?.AsInt() ?? 0);

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

                    EmbedBuilder embed = Utils.Embed;
                    embed.Title = e.Locale.GetString("miki_module_pasta_search_header");
                    embed.Description = resultString;

                    embed.SetFooter(e.Locale.GetString("page_index", page + 1, (Math.Ceiling((double)totalCount / 25)).ToString()));

                    embed.ToEmbed().QueueToChannel(e.Channel);
                    return;
                }

                e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_search_error_no_results", query))
					.ToEmbed().QueueToChannel(e.Channel);
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

        //[Command(Name = "reportpasta")]
        //public async Task ReportPastaAsync(EventContext e)
        //{
        //    ArgObject arg = e.Arguments.FirstOrDefault();

        //    if(arg == null)
	//    {
	//        // TODO: error message
	//        return;
	//    }

	//    string pastaId = arg.Argument;
	//    arg = arg.Next();

	//    string reason = arg?.TakeUntilEnd()?.Argument ?? "";

	//    if(string.IsNullOrEmpty(reason))
	//    {
	//        // TODO: reason empty error
	//        return;
	//    }

	//    Utils.SuccessEmbed(e.Channel.Id, "Your report has been received!").QueueToChannel(e.Channel);

	//    Utils.Embed.SetAuthor(e.Author.Username, e.Author.GetAvatarUrl(), "")
	//        .SetDescription($"Reported pasta `{pastaId}`.```{reason}```")
	//        .SetColor(255, 0 , 0)
	//        .SetFooter(DateTime.Now.ToString(), "")
	//        .ToEmbed().QueueToChannel(Bot.Instance.Client.GetChannel(PastaReportsChannelId) as IMessageChannel);
	//}

        public async Task FavouritePastaList(EventContext e, bool lovedPastas = true)
        {
            IDiscordUser targetUser = e.Author;
            float totalPerPage = 25f;
            int page = 0;

            ArgObject arg = e.Arguments.FirstOrDefault();

            if (arg == null)
            {
                // TODO: error no user found
                return;
            }

            IDiscordUser user = await arg.GetUserAsync(e.Guild);

            if(user != null)
            {
                arg = arg.Next();
            }
            else
            {
                user = e.Author;
            }

            page = arg.AsInt() ?? 0;

            using (MikiContext context = new MikiContext())
            {
                long authorId = targetUser.Id.ToDbLong();
                IEnumerable<PastaVote> pastaVotes = context.Votes.Where(x => x.UserId == authorId && x.PositiveVote == lovedPastas);

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
                    Utils.ErrorEmbed(e, errorString).ToEmbed().QueueToChannel(e.Channel);
                    return;
                }

                EmbedBuilder embed = Utils.Embed;
                List<PastaVote> neededPastas = pastaVotes.Skip((int)totalPerPage * page).Take((int)totalPerPage).ToList();

                string resultString = string.Join(" ", neededPastas.Select(x => $"`{x.Id}`"));

                string useName = string.IsNullOrEmpty(targetUser.Username) ? targetUser.Username : targetUser.Username;
                embed.SetTitle($"{(lovedPastas ? e.Locale.GetString("miki_module_pasta_loved_header") : e.Locale.GetString("miki_module_pasta_hated_header"))} - {useName}");
                embed.SetDescription(resultString);
                embed.SetFooter(e.Locale.GetString("page_index", page + 1, Math.Ceiling(pastaVotes.Count() / totalPerPage)), "");

                embed.ToEmbed().QueueToChannel(e.Channel);
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
            string pastaName = e.Arguments.First().Argument;

            using (var context = new MikiContext())
            {
                var pasta = await context.Pastas.FindAsync(pastaName);

                if (pasta == null)
                {
                    e.ErrorEmbed(e.Locale.GetString("miki_module_pasta_error_null")).ToEmbed().QueueToChannel(e.Channel);
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
                        Id = pastaName, UserId = e.Author.Id.ToDbLong(), PositiveVote = vote
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

                e.SuccessEmbed(e.Locale.GetString("miki_module_pasta_vote_success", votecount.Upvotes - votecount.Downvotes)).QueueToChannel(e.Channel);
            }
        }
    }
}
