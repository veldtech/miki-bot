using Discord;
using IA;
using IA.Events;
using IA.Node;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Accounts;
using Miki.Accounts.Achievements;
using Miki.Core;
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
    internal class PastaModule
    {
        public async Task LoadEvents(Bot bot)
        {
            IModule module_general = new Module(m =>
            {
                m.Name = "Pasta";
                m.Events = new List<ICommandEvent>()
                {
                    new RuntimeCommandEvent("pasta")
                        .Default(async (e, args)        => await GetPasta(e, args))
                        .On("+", async (e, args)        => await CreatePasta(e, args))
                        .On("new", async(e, args)       => await CreatePasta(e, args))
                        .On("find", async(e, args)      => await SearchPasta(e, args))
                        .On("search", async(e, args)    => await SearchPasta(e, args))
                        .On("edit", async(e, args)      => await EditPasta(e, args))
                        .On("change", async(e, args)    => await EditPasta(e, args))
                        .On("remove", async(e, args)    => await DeletePasta(e, args))
                        .On("delete", async(e, args)    => await DeletePasta(e, args))
                        .On("who", async(e, args)       => await IdentifyPasta(e, args))
                        .On("?", async(e, args)         => await IdentifyPasta(e, args))
                        .On("whom", async(e, args)      => await IdentifyPasta(e, args))
                        .On("upvote", async(e, args)    => await VotePasta(e, args, true))
                        .On("love", async(e, args)      => await VotePasta(e, args, true))
                        .On("<3", async(e, args)        => await VotePasta(e, args, true))
                        .On("downvote", async(e, args)  => await VotePasta(e, args, false))
                        .On("</3", async(e, args)       => await VotePasta(e, args, false))
                        .On("hate", async(e, args)      => await VotePasta(e, args, false))
                        .On("mine", async(e, args)      => await MyPasta(e, args))
                        .On("my", async(e, args)        => await MyPasta(e, args))
                };
            });

            await new RuntimeModule(module_general).InstallAsync(bot);

            // Internal::AddCommandDone
            bot.Events.AddCommandDoneEvent(x =>
            {
                x.Name = "--count-commands";
                x.processEvent = async (m, e, s) =>
                {
                    if (s)
                    {
                        using (var context = new MikiContext())
                        {
                            CommandUsage u = await context.CommandUsages.FindAsync(m.Author.Id.ToDbLong(), e.Name);
                            if(u == null)
                            {
                                u = context.CommandUsages.Add(new CommandUsage() { UserId = m.Author.Id.ToDbLong(), Amount = 1, Name = e.Name });
                            }
                            else
                            {
                                u.Amount++;
                            }

                            User user = await context.Users.FindAsync(m.Author.Id.ToDbLong());
                            user.Total_Commands++;

                            await context.SaveChangesAsync();
                        }
                    }
                };
            });
        }

        private async Task MyPasta(IDiscordMessage e, string args)
        {
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            int page = 0;
            if (!string.IsNullOrWhiteSpace(args))
            {
                if (int.TryParse(args, out page))
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

                    IDiscordEmbed embed = e.CreateEmbed();
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

        private async Task CreatePasta(IDiscordMessage e, string args)
        {
            List<string> arguments = args.Split(' ').ToList();

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

        private async Task DeletePasta(IDiscordMessage e, string v)
        {
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            if (string.IsNullOrWhiteSpace(v))
            {
                await Utils.ErrorEmbed(locale, "Please specify which pasta you'd like to remove.")
                    .SendToChannel(e.Channel.Id);
                return;
            }

            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();

                GlobalPasta pasta = await context.Pastas.FindAsync(v);

                if(pasta == null)
                {
                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "This pasta doesn't exist! check the tag!"));
                    return;
                }

                if(pasta.CanDeletePasta(e.Author.Id))
                {
                    context.Pastas.Remove(pasta);

                    List<PastaVote> votes = context.Votes.AsNoTracking().Where(p => p.Id.Equals(v)).ToList();
                    context.Votes.RemoveRange(votes);

                    await context.SaveChangesAsync();

                    await e.Channel.SendMessage(Utils.SuccessEmbed(locale, $"Deleted pasta `{v}`!"));
                    return;
                }
                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "This pasta is not yours!"));
                return;
            }
        }

        private async Task EditPasta(IDiscordMessage e, string content)
        {
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            if (string.IsNullOrWhiteSpace(content))
            {
                await Utils.ErrorEmbed(locale, "Please specify which pasta you'd like to edit.")
                    .SendToChannel(e.Channel.Id);
                return;
            }

            if(content.Split(' ').Length == 1)
            {
                await Utils.ErrorEmbed(locale, "Please specify the content you'd like it to be edited to.")
                    .SendToChannel(e.Channel.Id);
                return;
            }

            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();

                string tag = content.Split(' ')[0];
                content = content.Substring(tag.Length + 1);

                GlobalPasta p = await context.Pastas.FindAsync(tag);

                if (p.CreatorId == e.Author.Id || Bot.instance.Events.Developers.Contains(e.Author.Id))
                {
                    p.Text = content;
                    await context.SaveChangesAsync();
                    await e.Channel.SendMessage($"Edited `{tag}`!");
                }
                else
                {
                    await e.Channel.SendMessage($@"You cannot edit pastas you did not create. Baka!");
                }
            }
        }

        private async Task GetPasta(IDiscordMessage e, string args)
        {
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            if (string.IsNullOrWhiteSpace(args))
            {
                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "Please enter one of the tags, or commands."));
                return;
            }

            List<string> arguments = args.Split(' ').ToList();
            
            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();

                GlobalPasta pasta = await context.Pastas.FindAsync(arguments[0]);
                if (pasta == null)
                {
                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, $"No pasta found with the name `{args}`"));
                    return;
                }
                pasta.TimesUsed++;
                await e.Channel.SendMessage(pasta.Text);
                await context.SaveChangesAsync();
            }
        }

        private async Task IdentifyPasta(IDiscordMessage e, string args)
        {
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            if (string.IsNullOrWhiteSpace(args))
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
                    GlobalPasta pasta = await context.Pastas.FindAsync(args);

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

        private async Task SearchPasta(IDiscordMessage e, string args)
        {
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            if(string.IsNullOrWhiteSpace(args))
            {
                await Utils.ErrorEmbed(locale, "Please specify the terms you want to search.")
                    .SendToChannel(e.Channel.Id);
                return;
            }

            List<string> arguments = args.Split(' ').ToList();
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

                    IDiscordEmbed embed = e.CreateEmbed();
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

        private async Task VotePasta(IDiscordMessage e, string tag, bool vote)
        {
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            using (var context = MikiContext.CreateNoCache())
            {
                context.Set<GlobalPasta>().AsNoTracking();

                var pasta = await context.Pastas.FindAsync(tag);

                if(pasta == null)
                {
                    await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "This pasta doesn't exist :(("));
                    return;
                }

                long authorId = e.Author.Id.ToDbLong();

                var voteObject = context.Votes.AsNoTracking().Where(q => q.Id == tag && q.__UserId == authorId).FirstOrDefault();

                if (voteObject == null)
                {
                    voteObject = new PastaVote() { Id = tag, __UserId = e.Author.Id.ToDbLong(), PositiveVote = vote };
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