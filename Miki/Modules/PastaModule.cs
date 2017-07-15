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
                List<LeaderboardsItem> leaderboards = d.Database.SqlQuery<LeaderboardsItem>("select TOP 12 Id as Name, TimesUsed as Value from dbo.GlobalPastas as c order by Value DESC").ToList();

                IDiscordEmbed e = Utils.Embed
                    .SetTitle("Most popular pastas")
                    .SetColor(new IA.SDK.Color(1, 0.6f, 0.2f));

                foreach (LeaderboardsItem t in leaderboards)
                {
                    e.AddInlineField(t.Name, (t == leaderboards.First() ? "👑 " + t.Value.ToString() : "✨ " + t.Value.ToString()));
                }

                await e.SendToChannel(context.Channel.Id);
            }
        }

        [Command(Name = "toppasta")]
        public async Task DoPastaLeaderboardsLove(EventContext context)
        {
            using (var d = new MikiContext())
            {
                List<LeaderboardsItem> leaderboards = d.Database.SqlQuery<LeaderboardsItem>("select TOP 12 Id as Name, ((SELECT Count(*) from Votes where Id = c.Id AND PositiveVote = 1) - (SELECT Count(*) from Votes where Id = c.Id AND PositiveVote = 0)) as Value from dbo.GlobalPastas as c order by Value DESC").ToList();

                IDiscordEmbed e = Utils.Embed
                    .SetTitle("Top pastas")
                    .SetColor(new IA.SDK.Color(1, 0, 0));
                
                foreach(LeaderboardsItem t in leaderboards)
                {
                    e.AddInlineField(t.Name, (t == leaderboards.First() ? "💖 " + t.Value.ToString() : (t.Value < 0 ? "💔 " : "❤ ") + t.Value.ToString()));
                }

                await e.SendToChannel(context.Channel.Id);
            }
        }

        [Command(Name = "mypasta")]
        public async Task MyPasta(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            int page = 0;
            if (!string.IsNullOrWhiteSpace(e.arguments))
            {
                if (int.TryParse(e.arguments, out page))
                {
                    page -= 1;
                }
            }

            long authorId = e.Author.Id.ToDbLong();

            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();

                var pastasFound = context.Database.SqlQuery<PastaSearchResult>("select [GlobalPastas].id, count(*) OVER() AS total_count from [GlobalPastas] where CreatorID = @p0 ORDER BY id OFFSET @p1 ROWS FETCH NEXT 25 ROWS ONLY;",
                    authorId, page * 25).ToList();

                if (pastasFound?.Count > 0)
                {
                    string resultString = "";

                    pastasFound.ForEach(x => { resultString += "`" + x.Id + "` "; });

                    IDiscordEmbed embed = Utils.Embed;
                    embed.Title = e.Author.Username + "'s pastas";
                    embed.Description = resultString;
                    embed.CreateFooter();
                    embed.Footer.Text = $"page {page + 1} of {(Math.Ceiling((double)pastasFound[0].Total_Count / 25)).ToString()}";

                    await e.Channel.SendMessage(embed);
                    return;
                }
                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, $"Sorry, but you don't have any pastas yet.."));
            }
        }

        [Command(Name = "createpasta")]
        public async Task CreatePasta(EventContext e)
        {
            List<string> arguments = e.arguments.Split(' ').ToList();

            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            if(arguments.Count < 2)
            {
                await Utils.ErrorEmbed(locale, "I couldn't find any content for this pasta, please specify what you want to make.").SendToChannel(e.Channel.Id);
                return;
            }

            string id = arguments[0];
            arguments.RemoveAt(0);

            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();

                try
                {
                    GlobalPasta pasta = await context.Pastas.FindAsync(id);

                    if (pasta != null)
                    {
                        await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "This pasta already exist! try a different tag!"));
                        return;
                    }

                    context.Pastas.Add(new GlobalPasta() { Id = id, Text = string.Join(" ", arguments), CreatorId = e.Author.Id, date_created = DateTime.Now });
                    await context.SaveChangesAsync();
                    await e.Channel.SendMessage(Utils.SuccessEmbed(locale, $"Created pasta `{id}`!"));
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
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            if (string.IsNullOrWhiteSpace(e.arguments))
            {
                await Utils.ErrorEmbed(locale, "Please specify which pasta you'd like to remove.")
                    .SendToChannel(e.Channel.Id);
                return;
            }

            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();

                GlobalPasta pasta = await context.Pastas.FindAsync(e.arguments);

                if(pasta == null)
                {
                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "This pasta doesn't exist! check the tag!"));
                    return;
                }

                if(pasta.CanDeletePasta(e.Author.Id))
                {
                    context.Pastas.Remove(pasta);

                    List<PastaVote> votes = context.Votes.AsNoTracking().Where(p => p.Id.Equals(e.arguments)).ToList();
                    context.Votes.RemoveRange(votes);

                    await context.SaveChangesAsync();

                    await e.Channel.SendMessage(Utils.SuccessEmbed(locale, $"Deleted pasta `{e.arguments}`!"));
                    return;
                }
                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "This pasta is not yours!"));
                return;
            }
        }

        [Command(Name = "editpasta")]
        public async Task EditPasta(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            if (string.IsNullOrWhiteSpace(e.arguments))
            {
                await Utils.ErrorEmbed(locale, "Please specify which pasta you'd like to edit.")
                    .SendToChannel(e.Channel.Id);
                return;
            }

            if(e.arguments.Split(' ').Length == 1)
            {
                await Utils.ErrorEmbed(locale, "Please specify the content you'd like it to be edited to.")
                    .SendToChannel(e.Channel.Id);
                return;
            }

            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();

                string tag = e.arguments.Split(' ')[0];
                e.arguments = e.arguments.Substring(tag.Length + 1);

                GlobalPasta p = await context.Pastas.FindAsync(tag);

                if (p.CreatorId == e.Author.Id || Bot.instance.Events.Developers.Contains(e.Author.Id))
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
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            if (string.IsNullOrWhiteSpace(e.arguments))
            {
                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "Please enter one of the tags, or commands."));
                return;
            }

            List<string> arguments = e.arguments.Split(' ').ToList();
            
            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();

                GlobalPasta pasta = await context.Pastas.FindAsync(arguments[0]);
                if (pasta == null)
                {
                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, $"No pasta found with the name `{e.arguments}`"));
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
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            if (string.IsNullOrWhiteSpace(e.arguments))
            {
                await Utils.ErrorEmbed(locale, "Please state which pasta you'd like to identify.")
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
                        await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "This pasta doesn't exist!"));
                        return;
                    }

                    User creator = await context.Users.FindAsync(pasta.creator_id);

                    EmbedBuilder b = new EmbedBuilder();
                    b.Author = new EmbedAuthorBuilder();
                    b.Author.Name = pasta.Id.ToUpper();
                    b.Color = new Discord.Color(47, 208, 192);

                    if (creator != null)
                    {
                        b.AddField(x =>
                        {
                            x.Name = "Created by";
                            x.Value = $"{creator.Name} [{creator.Id}]";
                            x.IsInline = true;
                        });
                    }

                    b.AddField(x =>
                    {
                        x.Name = "Date Created";
                        x.Value = pasta.date_created.ToShortDateString();
                        x.IsInline = true;
                    });

                    b.AddInlineField("Times Used", pasta.TimesUsed);

                    b.AddField(x =>
                    {
                        x.Name = "Rating";

                        VoteCount v = pasta.GetVotes(context);

                        x.Value = $"⬆️ {v.Upvotes} ⬇️ {v.Downvotes}";
                        x.IsInline = true;
                    });

                    await e.Channel.SendMessage(new RuntimeEmbed(b));

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
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            if(string.IsNullOrWhiteSpace(e.arguments))
            {
                await Utils.ErrorEmbed(locale, "Please specify the terms you want to search.")
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

            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();
                var pastasFound = context.Database.SqlQuery<PastaSearchResult>("select [GlobalPastas].Id, count(*) OVER() AS total_count from [GlobalPastas] where Id like @p0 ORDER BY id OFFSET @p1 ROWS FETCH NEXT 25 ROWS ONLY;",
                    "%" + arguments[0] + "%", page * 25).ToList();

                if (pastasFound?.Count > 0)
                {
                    string resultString = "";
                        
                    pastasFound.ForEach(x => { resultString += "`" + x.Id + "` "; });

                    IDiscordEmbed embed = Utils.Embed;
                    embed.Title = "🔎 I found these pastas";
                    embed.Description = resultString;
                    embed.CreateFooter();
                    embed.Footer.Text = $"page {page + 1} of {(Math.Ceiling((double)pastasFound[0].Total_Count / 25)).ToString()}";

                    await e.Channel.SendMessage(embed);
                    return;
                }
                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, $"Sorry, but we couldn't find a pasta with `{arguments[0]}`"));
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
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();

                var pasta = await context.Pastas.FindAsync(e.arguments);

                if(pasta == null)
                {
                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "This pasta doesn't exist :(("));
                    return;
                }

                long authorId = e.Author.Id.ToDbLong();

                var voteObject = context.Votes.AsNoTracking().Where(q => q.Id == e.arguments && q.__UserId == authorId).FirstOrDefault();

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

                await e.Channel.SendMessage(Utils.SuccessEmbed(locale, $"Your vote has been updated!\nCurrent Score: `{votecount.Upvotes - votecount.Downvotes}`"));
            }
        }
    }
}