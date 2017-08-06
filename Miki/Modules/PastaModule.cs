using Discord;
using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.Node;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Accounts;
using Miki.Accounts.Achievements;
using Miki.Languages;
using Miki.Models;
using Miki.Objects;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Objects;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module("pasta")]
    public class PastaModule
    {
        [Command(Name = "poppasta")]
        public async Task DoPastaLeaderboardsPopular(EventContext context)
        {
            using (var d = new MikiContext())
            {
                List<GlobalPasta> leaderboards = d.Pastas.OrderByDescending(x => x.TimesUsed)
                                                         .Take(12)
                                                         .ToList();
                    
                IDiscordEmbed e = Utils.Embed
                    .SetTitle(context.GetResource("poppasta_title"))
                    .SetColor(new IA.SDK.Color(1, 0.6f, 0.2f));

                foreach (GlobalPasta t in leaderboards)
                {
                    e.AddInlineField(t.Id, (t == leaderboards.First() ? "👑 " + t.TimesUsed.ToString() : "✨ " + t.TimesUsed.ToString()));
                }

                await e.SendToChannel(context.Channel.Id);
            }
        }

        [Command(Name = "toppasta")]
        public async Task DoPastaLeaderboardsLove(EventContext context)
        {
            using (var d = new MikiContext())
            {
                List<GlobalPasta> leaderboards = d.Pastas.OrderByDescending(x => d.Votes.Where(p => p.Id == x.Id).Count())
                                                      .Take(12)
                                                      .ToList();

                IDiscordEmbed e = Utils.Embed
                    .SetTitle(context.GetResource("toppasta_title"))
                    .SetColor(new IA.SDK.Color(1, 0, 0));
                
                foreach(GlobalPasta t in leaderboards)
                {
                    int amount = d.Votes.Where(p => p.Id == t.Id).Count();
                    e.AddInlineField(t.Id, (t == leaderboards.First() ? "💖 " + amount : (amount < 0 ? "💔 " : "❤ ") + amount));
                }

                await e.SendToChannel(context.Channel.Id);
            }
        }

        [Command(Name = "mypasta")]
        public async Task MyPasta(EventContext e)
        {
            
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            int page = 0;
            if (!string.IsNullOrWhiteSpace(e.arguments))
            {
                List<string> arguments = e.arguments.Split(' ').ToList();
                if (int.TryParse(arguments[0], out page) || int.TryParse(arguments[0], out page))
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
                var pastasFound = context.Pastas.Where(x => x.creator_id == userId)
                                                .OrderByDescending(x => x.Id)
                                                .Skip(page * 25)
                                                .Take(25)
                                                .ToList();

                var totalCount = context.Pastas.Where(x => x.creator_id == userId)
                                               .Count();

                if(page * 25 > totalCount)
                {
                    await Utils.ErrorEmbed(locale, e.GetResource("pasta_error_out_of_index"))
                        .SendToChannel(e.Channel);
                    return;
                }

                if (pastasFound?.Count > 0)
                {
                    string resultString = "";

                    pastasFound.ForEach(x => { resultString += "`" + x.Id + "` "; });

                    await Utils.Embed
                        .SetTitle(e.GetResource("mypasta_title", userName))
                        .SetDescription(resultString)
                        .SetFooter(e.GetResource("pasta_page_index", page + 1, (Math.Ceiling((double)totalCount / 25)).ToString()), null)
                        .SendToChannel(e.Channel);
                    return;
                }

                await Utils.ErrorEmbed(locale, e.GetResource("mypasta_error_no_pastas"))
                    .SendToChannel(e.Channel);
            }
        }

        [Command(Name = "createpasta")]
        public async Task CreatePasta(EventContext e)
        {
            List<string> arguments = e.arguments.Split(' ').ToList();

            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if(arguments.Count < 2)
            {
                await Utils.ErrorEmbed(locale, e.GetResource("createpasta_error_no_content")).SendToChannel(e.Channel.Id);
                return;
            }

            string id = arguments[0];
            arguments.RemoveAt(0);

            using (var context = new MikiContext())
            {
                try
                {
                    GlobalPasta pasta = await context.Pastas.FindAsync(id);

                    if (pasta != null)
                    {
                        await Utils.ErrorEmbed(locale, e.GetResource("miki_module_pasta_create_error_already_exist")).SendToChannel(e.Channel);
                        return;
                    }

                    context.Pastas.Add(new GlobalPasta() { Id = id, Text = string.Join(" ", arguments), creator_id = e.Author.Id.ToDbLong(), date_created = DateTime.Now });
                    await context.SaveChangesAsync();
                    await Utils.SuccessEmbed(locale, e.GetResource("miki_module_pasta_create_success", id)).SendToChannel(e.Channel);
                }
                catch (Exception ex)
                {
                    Log.ErrorAt("IdentifyPasta", ex.Message);
                }
            }
        }

        [Command(Name = "deletepasta")]
        public async Task DeletePasta(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if (string.IsNullOrWhiteSpace(e.arguments))
            {
                await Utils.ErrorEmbed(locale, e.GetResource("miki_module_pasta_error_specify", e.GetResource("miki_module_pasta_error_specify")))
                    .SendToChannel(e.Channel.Id);
                return;
            }

            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();

                GlobalPasta pasta = await context.Pastas.FindAsync(e.arguments);

                if(pasta == null)
                {
                    await Utils.ErrorEmbed(locale, e.GetResource("miki_module_pasta_error_null")).SendToChannel(e.Channel);
                    return;
                }

                if(pasta.CanDeletePasta(e.Author.Id))
                {
                    context.Pastas.Remove(pasta);

                    List<PastaVote> votes = context.Votes.AsNoTracking().Where(p => p.Id.Equals(e.arguments)).ToList();
                    context.Votes.RemoveRange(votes);

                    await context.SaveChangesAsync();

                    await Utils.SuccessEmbed(locale, e.GetResource("miki_module_pasta_delete_success", e.arguments)).SendToChannel(e.Channel);
                    return;
                }
                await Utils.ErrorEmbed(locale, e.GetResource("miki_module_pasta_error_no_permissions", e.GetResource("miki_module_pasta_error_specify_delete"))).SendToChannel(e.Channel);
                return;
            }
        }

        [Command(Name = "editpasta")]
        public async Task EditPasta(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if (string.IsNullOrWhiteSpace(e.arguments))
            {
                await Utils.ErrorEmbed(locale, e.GetResource("miki_module_pasta_error_specify", e.GetResource("miki_module_pasta_error_specify_edit")))
                    .SendToChannel(e.Channel.Id);
                return;
            }

            if(e.arguments.Split(' ').Length == 1)
            {
                await Utils.ErrorEmbed(locale, e.GetResource("miki_module_pasta_error_specify", e.GetResource("miki_module_pasta_error_specify_edit")))
                    .SendToChannel(e.Channel.Id);
                return;
            }

            using (var context = new MikiContext())
            {
                string tag = e.arguments.Split(' ')[0];
                e.arguments = e.arguments.Substring(tag.Length + 1);

                GlobalPasta p = await context.Pastas.FindAsync(tag);

                if (p.creator_id == e.Author.Id.ToDbLong() || Bot.instance.Events.Developers.Contains(e.Author.Id))
                {
                    p.Text = e.arguments;
                    await context.SaveChangesAsync();
                    await e.Channel.SendMessage($"Edited `{tag}`!");
                }
                else
                {
                    await e.Channel.SendMessage($@"You cannot edit pastas you did not create. Baka!");
                }
            }
        }

        [Command(Name = "pasta")]
        public async Task GetPasta(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if (string.IsNullOrWhiteSpace(e.arguments))
            {
                await Utils.ErrorEmbed(locale, e.GetResource("pasta_error_no_arg")).SendToChannel(e.Channel);
                return;
            }

            List<string> arguments = e.arguments.Split(' ').ToList();
            
            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();

                GlobalPasta pasta = await context.Pastas.FindAsync(arguments[0]);
                if (pasta == null)
                {
                    await Utils.ErrorEmbed(locale, e.GetResource("miki_module_pasta_search_error_no_results", e.arguments)).SendToChannel(e.Channel);
                    return;
                }
                pasta.TimesUsed++;
                await e.Channel.SendMessage(pasta.Text);
                await context.SaveChangesAsync();
            }
        }

        [Command(Name = "infopasta")]
        public async Task IdentifyPasta(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if (string.IsNullOrWhiteSpace(e.arguments))
            {
                await Utils.ErrorEmbed(locale, e.GetResource("infopasta_error_no_arg"))
                    .SendToChannel(e.Channel.Id);
                return;
            }

            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();

                try
                {
                    GlobalPasta pasta = await context.Pastas.FindAsync(e.arguments);

                    if(pasta == null)
                    {
                        await Utils.ErrorEmbed(locale, e.GetResource("miki_module_pasta_error_null")).SendToChannel(e.Channel);
                        return;
                    }

                    User creator = await context.Users.FindAsync(pasta.creator_id);

                    IDiscordEmbed b = Utils.Embed;

                    b.SetAuthor(pasta.Id.ToUpper(), "", "");
                    b.Color = new IA.SDK.Color(47, 208, 192);
                   
                    if (creator != null)
                    {
                        b.AddInlineField(e.GetResource("miki_module_pasta_identify_created_by"), $"{ creator.Name} [{creator.Id}]");
                    }

                    b.AddInlineField(e.GetResource("miki_module_pasta_identify_date_created"), pasta.date_created.ToShortDateString());

                    b.AddInlineField(e.GetResource("miki_module_pasta_identify_times_used"), pasta.TimesUsed.ToString());

                    VoteCount v = pasta.GetVotes(context);

                    b.AddInlineField(e.GetResource("infopasta_rating"), $"⬆️ { v.Upvotes} ⬇️ {v.Downvotes}");

                    await b.SendToChannel(e.Channel);

                }
                catch (Exception ex)
                {
                    Log.ErrorAt("IdentifyPasta", ex.Message);
                }
            }
        }

        [Command(Name = "searchpasta")]
        public async Task SearchPasta(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if(string.IsNullOrWhiteSpace(e.arguments))
            {
                await Utils.ErrorEmbed(locale, e.GetResource("searchpasta_error_no_arg"))
                    .SendToChannel(e.Channel.Id);
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

            using (var context = new MikiContext())
            {
                var pastasFound = context.Pastas.Where(x => x.Id.Contains(arguments[0]))
                                                .OrderByDescending(x => x.Id)
                                                .Skip(page * 25)
                                                .Take(25)
                                                .ToList();

                var totalCount = context.Pastas.Where(x => x.Id.Contains(arguments[0]))
                                               .Count();

                if (pastasFound?.Count > 0)
                {
                    string resultString = "";
                        
                    pastasFound.ForEach(x => { resultString += "`" + x.Id + "` "; });

                    IDiscordEmbed embed = Utils.Embed;
                    embed.Title = e.GetResource("miki_module_pasta_search_header");
                    embed.Description = resultString;
                    embed.CreateFooter();
                    embed.Footer.Text = e.GetResource("pasta_page_index", page + 1, (Math.Ceiling((double)totalCount / 25)).ToString());

                    await embed.SendToChannel(e.Channel);
                    return;
                }

                await Utils.ErrorEmbed(locale, e.GetResource("miki_module_pasta_search_error_no_results", arguments[0]))
                    .SendToChannel(e.Channel);
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
            Locale locale = Locale.GetEntity(e.Channel.Id);

            using (var context = new MikiContext())
            {
                var pasta = await context.Pastas.FindAsync(e.arguments);

                if(pasta == null)
                {
                    await Utils.ErrorEmbed(locale, e.GetResource("miki_module_pasta_error_null")).SendToChannel(e.Channel);
                    return;
                }

                long authorId = e.Author.Id.ToDbLong();

                var voteObject = context.Votes.Where(q => q.Id == e.arguments && q.__UserId == authorId)
                                              .FirstOrDefault();

                if (voteObject == null)
                {
                    voteObject = new PastaVote() { Id = e.arguments, __UserId = e.Author.Id.ToDbLong(), PositiveVote = vote };
                    context.Votes.Add(voteObject);
                }
                else
                {
                    voteObject.PositiveVote = vote;
                }
                await context.SaveChangesAsync();

                var votecount = pasta.GetVotes(context);

                await Utils.SuccessEmbed(locale, e.GetResource("miki_module_pasta_vote_success", votecount.Upvotes - votecount.Downvotes)).SendToChannel(e.Channel);
            }
        }
    }
}
