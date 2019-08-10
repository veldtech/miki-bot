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
using Miki.Framework.Events;
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

        [Command("mypasta")]
        public async Task MyPasta(IContext e)
        {
            if (e.GetArgumentPack().Take(out int page))
            {
                page--;
            }

            long userId;
            string userName;
            if (e.GetMessage().MentionedUserIds.Count() > 0)
            {
                userId = e.GetMessage().MentionedUserIds.First().ToDbLong();
                userName = (await e.GetGuild().GetMemberAsync(userId.FromDbLong())).Username;
            }
            else
            {
                userId = e.GetAuthor().Id.ToDbLong();
                userName = e.GetAuthor().Username;
            }

            var context = e.GetService<MikiDbContext>();

            var pastasFound = await context.Pastas.Where(x => x.CreatorId == userId)
                .OrderByDescending(x => x.Id)
                .Skip(page * 25)
                .Take(25)
                .ToListAsync();

            var totalCount = await context.Pastas.Where(x => x.CreatorId == userId)
                .CountAsync();

            if (page * 25 > totalCount)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("pasta_error_out_of_index"))
                    .ToEmbed().QueueAsync(e.GetChannel());
                return;
            }

            if (pastasFound?.Count > 0)
            {
                string resultString = "";

                pastasFound.ForEach(x => { resultString += "`" + x.Id + "` "; });

                await new EmbedBuilder()
                    .SetTitle(e.GetLocale().GetString("mypasta_title", userName))
                    .SetDescription(resultString)
                    .SetFooter(e.GetLocale().GetString("page_index", page + 1, (Math.Ceiling((double)totalCount / 25)).ToString()), null)
                    .ToEmbed().QueueAsync(e.GetChannel());
                return;
            }

            await e.ErrorEmbed(e.GetLocale().GetString("mypasta_error_no_pastas"))
                .ToEmbed().QueueAsync(e.GetChannel());
        }

        [Command("createpasta")]
        public async Task CreatePasta(IContext e)
        {
            if (e.GetArgumentPack().Pack.Length < 2)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("createpasta_error_no_content"))
                    .ToEmbed().QueueAsync(e.GetChannel());
                return;
            }

            e.GetArgumentPack().Take(out string id);
            string text = e.GetArgumentPack().Pack.TakeAll();

            if (Regex.IsMatch(text, "(http[s]://)?((discord.gg)|(discordapp.com/invite))/([A-Za-z0-9]+)", RegexOptions.IgnoreCase))
            {
                throw new PastaInviteException();
            }

            var context = e.GetService<MikiDbContext>();

            await GlobalPasta.AddAsync(context, id, text, (long)e.GetAuthor().Id);
            await context.SaveChangesAsync();


            await e.SuccessEmbed(e.GetLocale().GetString("miki_module_pasta_create_success", id))
                .QueueAsync(e.GetChannel());
        }

        [Command("deletepasta")]
        public async Task DeletePasta(IContext e)
        {
            string pastaArg = e.GetArgumentPack().Pack.TakeAll();

            if (string.IsNullOrWhiteSpace(pastaArg))
            {
                await e.ErrorEmbed(e.GetLocale().GetString("miki_module_pasta_error_specify", e.GetLocale().GetString("miki_module_pasta_error_specify")))
                    .ToEmbed().QueueAsync(e.GetChannel());
                return;
            }

            var context = e.GetService<MikiDbContext>();

            GlobalPasta pasta = await context.Pastas.FindAsync(pastaArg);

            if (pasta == null)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("miki_module_pasta_error_null")).ToEmbed().QueueAsync(e.GetChannel());
                return;
            }

            if (pasta.CreatorId == e.GetAuthor().Id.ToDbLong())
            {
                context.Pastas.Remove(pasta);

                List<PastaVote> votes = context.Votes.Where(p => p.Id == pastaArg).ToList();
                context.Votes.RemoveRange(votes);

                await context.SaveChangesAsync();

                await e.SuccessEmbed(e.GetLocale().GetString("miki_module_pasta_delete_success", pastaArg)).QueueAsync(e.GetChannel());
                return;
            }
            await e.ErrorEmbed(e.GetLocale().GetString("miki_module_pasta_error_no_permissions", e.GetLocale().GetString("miki_module_pasta_error_specify_delete")))
                .ToEmbed().QueueAsync(e.GetChannel());
        }

        [Command("editpasta")]
        public async Task EditPasta(IContext e)
        {
            if (e.GetArgumentPack().Pack.Length < 2)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("miki_module_pasta_error_specify", e.GetLocale().GetString("miki_module_pasta_error_specify_edit")))
                    .ToEmbed().QueueAsync(e.GetChannel());
                return;
            }

            var context = e.GetService<MikiDbContext>();

            e.GetArgumentPack().Take(out string tag);

            GlobalPasta p = await context.Pastas.FindAsync(tag);

            if (p.CreatorId == e.GetAuthor().Id.ToDbLong())
            {
                p.Text = e.GetArgumentPack().Pack.TakeAll();
                await context.SaveChangesAsync();
                await e.SuccessEmbed($"Edited `{tag}`!")
                    .QueueAsync(e.GetChannel());
            }
            else
            {
                await e.ErrorEmbed($"You cannot edit pastas you did not create. Baka!")
                    .ToEmbed().QueueAsync(e.GetChannel());
            }
        }

        [Command("pasta")]
        public async Task GetPasta(IContext e)
        {
            string pastaArg = e.GetArgumentPack().Pack.TakeAll();
            if (string.IsNullOrWhiteSpace(pastaArg))
            {
                await e.ErrorEmbed(e.GetLocale().GetString("pasta_error_no_arg")).ToEmbed().QueueAsync(e.GetChannel());
                return;
            }

            var context = e.GetService<MikiDbContext>();

            GlobalPasta pasta = await context.Pastas.FindAsync(pastaArg);
            if (pasta == null)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("miki_module_pasta_search_error_no_results", pastaArg))
                    .ToEmbed().QueueAsync(e.GetChannel());
                return;
            }
            pasta.TimesUsed++;

            var sanitizedText = Utils.EscapeEveryone(pasta.Text);
            e.GetChannel().QueueMessage(sanitizedText);
            await context.SaveChangesAsync();

        }

        [Command("infopasta")]
        public async Task IdentifyPasta(IContext e)
        {
            string pastaArg = e.GetArgumentPack().Pack.TakeAll();
            if (string.IsNullOrWhiteSpace(pastaArg))
            {
                await e.ErrorEmbed(e.GetLocale().GetString("infopasta_error_no_arg"))
                    .ToEmbed().QueueAsync(e.GetChannel());
                return;
            }

            var context = e.GetService<MikiDbContext>();

            GlobalPasta pasta = await context.Pastas.FindAsync(pastaArg);

            if (pasta == null)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("miki_module_pasta_error_null")).ToEmbed().QueueAsync(e.GetChannel());
                return;
            }

            User creator = await context.Users.FindAsync(pasta.CreatorId);

            EmbedBuilder b = new EmbedBuilder();

            b.SetAuthor(pasta.Id.ToUpper(), "", "");
            b.Color = new Color(47, 208, 192);

            if (creator != null)
            {
                b.AddInlineField(e.GetLocale().GetString("miki_module_pasta_identify_created_by"), $"{ creator.Name} [{creator.Id}]");
            }

            b.AddInlineField(e.GetLocale().GetString("miki_module_pasta_identify_date_created"), pasta.CreatedAt.ToShortDateString());

            b.AddInlineField(e.GetLocale().GetString("miki_module_pasta_identify_times_used"), pasta.TimesUsed.ToString());

            VoteCount v = await pasta.GetVotesAsync(context);

            b.AddInlineField(e.GetLocale().GetString("infopasta_rating"), $"⬆️ { v.Upvotes} ⬇️ {v.Downvotes}");

            await b.ToEmbed().QueueAsync(e.GetChannel());
        }

        [Command("searchpasta")]
        public async Task SearchPasta(IContext e)
        {
            if (!e.GetArgumentPack().Take(out string query))
            {
                await e.ErrorEmbed(e.GetLocale().GetString("searchpasta_error_no_arg"))
                    .ToEmbed().QueueAsync(e.GetChannel());
                return;
            }
            e.GetArgumentPack().Take(out int page);

            var context = e.GetService<MikiDbContext>();

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
                    Title = e.GetLocale().GetString("miki_module_pasta_search_header"),
                    Description = resultString
                }.SetFooter(e.GetLocale().GetString("page_index", page + 1, (Math.Ceiling((double)totalCount / 25)).ToString()))
                    .ToEmbed().QueueAsync(e.GetChannel());
                return;
            }

            await e.ErrorEmbed(e.GetLocale().GetString("miki_module_pasta_search_error_no_results", query))
                .ToEmbed().QueueAsync(e.GetChannel());
        }

        [Command("lovedpasta", "lovedpastas", "favouritepastas")]
        public async Task LovePastaList(IContext e)
        {
            await FavouritePastaList(e);
        }

        [Command("hatedpasta", "hatedpastas")]
        public async Task HatePastaList(IContext e)
        {
            await FavouritePastaList(e, false);
        }

        public async Task FavouritePastaList(IContext e, bool lovedPastas = true)
        {
            IDiscordUser targetUser = e.GetAuthor();
            float totalPerPage = 25f;

            e.GetArgumentPack().Take(out int page);

            var context = e.GetService<MikiDbContext>();

            long authorId = targetUser.Id.ToDbLong();
            List<PastaVote> pastaVotes = await context.Votes.Where(x => x.UserId == authorId && x.PositiveVote == lovedPastas).ToListAsync();

            int maxPage = (int)Math.Floor(pastaVotes.Count() / totalPerPage);
            page = page > maxPage ? maxPage : page;
            page = page < 0 ? 0 : page;

            if (pastaVotes.Count() <= 0)
            {
                string loveString = (lovedPastas ? e.GetLocale().GetString("miki_module_pasta_loved") : e.GetLocale().GetString("miki_module_pasta_hated"));
                string errorString = e.GetLocale().GetString("miki_module_pasta_favlist_self_none", loveString);
                if (e.GetMessage().MentionedUserIds.Count() >= 1)
                {
                    errorString = e.GetLocale().GetString("miki_module_pasta_favlist_mention_none", loveString);
                }
                await Utils.ErrorEmbed(e, errorString).ToEmbed()
                    .QueueAsync(e.GetChannel());
                return;
            }

            EmbedBuilder embed = new EmbedBuilder();
            List<PastaVote> neededPastas = pastaVotes.Skip((int)totalPerPage * page).Take((int)totalPerPage).ToList();

            string resultString = string.Join(" ", neededPastas.Select(x => $"`{x.Id}`"));

            string useName = string.IsNullOrEmpty(targetUser.Username) ? targetUser.Username : targetUser.Username;
            embed.SetTitle($"{(lovedPastas ? e.GetLocale().GetString("miki_module_pasta_loved_header") : e.GetLocale().GetString("miki_module_pasta_hated_header"))} - {useName}");
            embed.SetDescription(resultString);
            embed.SetFooter(
                e.GetLocale().GetString("page_index", page + 1, Math.Ceiling(pastaVotes.Count() / totalPerPage)),
                "");

            await embed.ToEmbed().QueueAsync(e.GetChannel());
        }

        [Command("lovepasta")]
        public async Task LovePasta(IContext e)
        {
            await VotePasta(e, true).ConfigureAwait(false);
        }

        [Command("hatepasta")]
        public async Task HatePasta(IContext e)
        {
            await VotePasta(e, false).ConfigureAwait(false);
        }

        private async Task VotePasta(IContext e, bool vote)
        {
            if (e.GetArgumentPack().Take(out string pastaName))
            {
                var context = e.GetService<MikiDbContext>();

                var pasta = await context.Pastas.FindAsync(pastaName);

                if (pasta == null)
                {
                    await e.ErrorEmbed(e.GetLocale().GetString("miki_module_pasta_error_null")).ToEmbed().QueueAsync(e.GetChannel());
                    return;
                }

                long authorId = e.GetAuthor().Id.ToDbLong();

                var voteObject = context.Votes
                    .Where(q => q.Id == pastaName && q.UserId == authorId)
                    .FirstOrDefault();

                if (voteObject == null)
                {
                    voteObject = new PastaVote()
                    {
                        Id = pastaName,
                        UserId = e.GetAuthor().Id.ToDbLong(),
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

                await e.SuccessEmbed(e.GetLocale().GetString("miki_module_pasta_vote_success", votecount.Upvotes - votecount.Downvotes)).QueueAsync(e.GetChannel());
            }
        }
    }
}