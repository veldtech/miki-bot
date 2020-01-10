namespace Miki.Modules
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
    using Miki.Framework.Extension;
    using Miki.Localization;
    using Miki.Modules.Accounts.Services;
    using Miki.Services;
    using Miki.Services.Achievements;
    using Miki.Utility;

    [Module("pastas")]
    public class PastaModule
    {
        [Command("createpasta")]
        public async Task CreatePasta(IContext e)
        {
            var locale = e.GetLocale();

            if(e.GetArgumentPack().Pack.Length < 2)
            {
                await e.ErrorEmbed(
                    locale.GetString("createpasta_error_no_content"))
                        .ToEmbed()
                        .QueueAsync(e, e.GetChannel());
                return;
            }

            var id = e.GetArgumentPack().TakeRequired<string>();
            var text = e.GetArgumentPack().Pack.TakeAll();

            if(Regex.IsMatch(
                text,
                "(http[s]://)?((discord.gg)|(discordapp.com/invite))/([A-Za-z0-9]+)",
                RegexOptions.IgnoreCase))
            {
                throw new PastaInviteException();
            }

            var pastaService = e.GetService<PastaService>();

            await pastaService.CreatePastaAsync(id, text, (long)e.GetAuthor().Id);

            await e.SuccessEmbed(
                locale.GetString("miki_module_pasta_create_success", id))
                .QueueAsync(e, e.GetChannel());

            var a = e.GetService<AchievementService>();
            await a.UnlockAsync(e, a.GetAchievement(AchievementIds.CreatePastaId), e.GetAuthor().Id);
        }

        [Command("deletepasta")]
        public async Task DeletePasta(IContext e)
        {
            string pastaArg = e.GetArgumentPack().Pack.TakeAll();

            if(string.IsNullOrWhiteSpace(pastaArg))
            {
                await e.ErrorEmbed(
                        e.GetLocale().GetString("miki_module_pasta_error_specify",
                        e.GetLocale().GetString("miki_module_pasta_error_specify")))
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }

            var context = e.GetService<PastaService>();
            await context.DeletePastaAsync(pastaArg, (long)e.GetAuthor().Id);

            await e.SuccessEmbed(
                e.GetLocale().GetString("miki_module_pasta_delete_success", pastaArg))
                .QueueAsync(e, e.GetChannel());

        }

        [Command("editpasta")]
        public async Task EditPasta(IContext e)
        {
            if(e.GetArgumentPack().Pack.Length < 2)
            {
                await e.ErrorEmbed(
                        e.GetLocale().GetString("miki_module_pasta_error_specify",
                        e.GetLocale().GetString("miki_module_pasta_error_specify_edit")))
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }

            var context = e.GetService<PastaService>();

            var tag = e.GetArgumentPack().TakeRequired<string>();
            var body = e.GetArgumentPack().Pack.TakeAll();

            await context.UpdatePastaAsync(tag, body, (long)e.GetAuthor().Id);
        }

        [Command("mypasta")]
        public async Task MyPasta(IContext e)
        {
            if (e.GetArgumentPack().Take(out int page))
            {
                page--;
            }


            long userId;
            string userName;
            if (e.GetMessage().MentionedUserIds.Any())
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
                    .ToEmbed().QueueAsync(e, e.GetChannel());
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
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }

            await e.ErrorEmbed(e.GetLocale().GetString("mypasta_error_no_pastas"))
                .ToEmbed().QueueAsync(e, e.GetChannel());
        }

        [Command("pasta")]
        public async Task GetPasta(IContext e)
        {
            string pastaArg = e.GetArgumentPack().Pack.TakeAll();
            if (string.IsNullOrWhiteSpace(pastaArg))
            {
                await e.ErrorEmbed(e.GetLocale().GetString("pasta_error_no_arg"))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            var pastaService = e.GetService<PastaService>();

            GlobalPasta pasta = await pastaService.GetPastaAsync(pastaArg);
       
            await pastaService.UseAsync(pasta);

            var sanitizedText = Utils.EscapeEveryone(pasta.Text);     
            e.GetChannel().QueueMessage(e, null, sanitizedText);     
        }

        [Command("infopasta")]
        public async Task IdentifyPasta(IContext e)
        {
            var locale = e.GetLocale();

            string pastaArg = e.GetArgumentPack().Pack.TakeAll();
            if(string.IsNullOrWhiteSpace(pastaArg))
            {
                await e.ErrorEmbed(
                    locale.GetString("infopasta_error_no_arg"))
                        .ToEmbed()
                        .QueueAsync(e, e.GetChannel());
                return;
            }

            var context = e.GetService<PastaService>();
            var userService = e.GetService<IUserService>();

            var pasta = await context.GetPastaAsync(pastaArg);
            var votes = await context.GetVotesAsync(pasta.Id);
            var creator = await userService.GetUserAsync(pasta.CreatorId);

            EmbedBuilder b = new EmbedBuilder();

            b.SetAuthor(pasta.Id.ToUpper(), "", "");
            b.Color = new Color(47, 208, 192);

            if (creator != null)
            {
                b.AddInlineField(
                    locale.GetString("miki_module_pasta_identify_created_by"),
                    $"{ creator.Name} [{creator.Id}]");
            }

            b.AddInlineField(
                locale.GetString("miki_module_pasta_identify_date_created"), 
                pasta.CreatedAt.ToShortDateString());

            b.AddInlineField(
                locale.GetString("miki_module_pasta_identify_times_used"), 
                pasta.TimesUsed.ToString());

            b.AddInlineField(
                locale.GetString("infopasta_rating"), 
                $"⬆️ { votes.Upvotes} ⬇️ {votes.Downvotes}");

            await b.ToEmbed().QueueAsync(e, e.GetChannel());
        }

        [Command("searchpasta")]
        public async Task SearchPasta(IContext e)
        {
            if (!e.GetArgumentPack().Take(out string query))
            {
                await e.ErrorEmbed(e.GetLocale().GetString("searchpasta_error_no_arg"))
                    .ToEmbed().QueueAsync(e, e.GetChannel());
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
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }

            await e.ErrorEmbed(e.GetLocale().GetString("miki_module_pasta_search_error_no_results", query))
                .ToEmbed().QueueAsync(e, e.GetChannel());
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
                    .QueueAsync(e, e.GetChannel());
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

            await embed.ToEmbed().QueueAsync(e, e.GetChannel());
        }

        [Command("lovepasta")]
        public async Task LovePasta(IContext e)
        {
            await VotePasta(e, true)
                .ConfigureAwait(false);
        }

        [Command("hatepasta")]
        public async Task HatePasta(IContext e)
        {
            await VotePasta(e, false)
                .ConfigureAwait(false);
        }

        private async Task VotePasta(IContext e, bool vote)
        {
            if (e.GetArgumentPack().Take(out string pastaName))
            {
                var context = e.GetService<MikiDbContext>();
                var pastaService = e.GetService<PastaService>();

                long authorId = e.GetAuthor().Id.ToDbLong();

                await pastaService.VoteAsync(new PastaVote()
                {
                    Id = pastaName,
                    UserId = e.GetAuthor().Id.ToDbLong(),
                    PositiveVote = vote
                });

                var votes = await pastaService.GetVotesAsync(pastaName);

                await e.SuccessEmbed(
                    e.GetLocale().GetString("miki_module_pasta_vote_success",
                    votes.Upvotes - votes.Downvotes))
                    .QueueAsync(e, e.GetChannel());
            }
        }
    }
}