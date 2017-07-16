using Discord.WebSocket;
using IA;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Accounts;
using Miki.Languages;
using Miki.Models;
using Miki.Models.Objects.Guild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module("Guild Accounts")]
    class GuildAccountsModule
    {
        [Command(Name = "guildweekly")]
        public async Task GuildWeekly(EventContext context)
        {
            using (MikiContext database = new MikiContext())
            {
                Locale locale = Locale.GetEntity(context.Channel.Id);
                LocalExperience thisUser = await database.Experience.FindAsync(context.Guild.Id.ToDbLong(), context.Author.Id.ToDbLong());
                GuildUser thisGuild = await database.GuildUsers.FindAsync(context.Guild.Id.ToDbLong());
                Timer timer = await database.Timers.FindAsync(context.Guild.Id.ToDbLong(), context.Author.Id.ToDbLong());

                if (thisUser == null)
                {
                    Log.ErrorAt("Guildweekly", "User is null");
                    return;
                }

                if (thisGuild == null)
                {
                    Log.ErrorAt("Guildweekly", "Guild is null");
                    return;
                }

                if (thisUser.Experience > thisGuild.MinimalExperienceToGetRewards)
                {
                    if (timer == null)
                    {
                        timer = database.Timers.Add(new Timer()
                        {
                            GuildId = context.Guild.Id.ToDbLong(),
                            UserId = context.Author.Id.ToDbLong(),
                            Value = DateTime.Now.AddDays(-30)
                        });
                        await database.SaveChangesAsync();
                    }

                    if (timer.Value.AddDays(7) <= DateTime.Now)
                    {
                        SocketGuild guild = Bot.instance.Client.GetGuild(thisGuild.Id.FromDbLong());

                        GuildUser rival = await thisGuild.GetRival();

                        if (rival == null)
                        {
                            await Utils.Embed
                                .SetTitle(locale.GetString("miki_terms_weekly"))
                                .SetDescription("You have no rival yet, Server admins: use `>guildnewrival` to start matchmaking!")
                                .SendToChannel(context.Channel);
                            return;
                        }

                        if (rival.Experience > thisGuild.Experience)
                        {
                            await Utils.Embed
                                .SetTitle(locale.GetString("miki_terms_weekly"))
                                .SetDescription("you got to have a higher level than your rival!")
                                .SendToChannel(context.Channel);
                            return;
                        }

                        int mekosGained = (int)Math.Round((((Global.random.NextDouble() + 1.25) * 0.5) * 10) * thisGuild.CalculateLevel(thisGuild.Experience));

                        User user = await database.Users.FindAsync(context.Author.Id.ToDbLong());
                        user.Currency += mekosGained;

                        await Utils.Embed
                            .SetTitle(locale.GetString("miki_terms_weekly"))
                            .AddInlineField("Mekos", mekosGained.ToString())
                            .SendToChannel(context.Channel);

                        timer.Value = DateTime.Now;
                        await database.SaveChangesAsync();
                    }
                    else
                    {
                        await Utils.Embed
                            .SetTitle(locale.GetString("miki_terms_weekly"))
                            .SetDescription("You've already used your weekly, available again in " + (timer.Value.AddDays(7) - DateTime.Now).ToTimeString())
                            .SendToChannel(context.Channel);
                    }
                }
                else
                {
                    await Utils.Embed
                        .SetTitle(locale.GetString("miki_terms_weekly"))
                        .SetDescription(locale.GetString("miki_guildweekly_insufficient_exp", thisGuild.MinimalExperienceToGetRewards))
                        .SendToChannel(context.Channel);
                }
            }
        }

        [Command(Name = "guildnewrival", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task GuildNewRival(EventContext context)
        {
            using (MikiContext db = new MikiContext())
            {
                GuildUser thisGuild = await db.GuildUsers.FindAsync(context.Guild.Id.ToDbLong());

                if(thisGuild.LastRivalRenewed.AddDays(1) > DateTime.Now)
                {
                    await Utils.Embed
                       .SetTitle("Rival")
                       .SetDescription($"Please wait a day before doing this command again!")
                       .SendToChannel(context.Channel);
                    return;
                }

                List<GuildUser> rivalGuilds = db.GuildUsers.Where((g) => Math.Abs(g.UserCount - thisGuild.UserCount) < (g.UserCount / 4) && g.RivalId == 0 && g.Id != thisGuild.Id).ToList();

                if (rivalGuilds.Count == 0)
                {
                    await Utils.Embed
                        .SetTitle("Whoopsie!")
                        .SetDescription("We couldn't matchmake you right now, try again later!")
                        .SendToChannel(context.Channel);
                    return;
                }

                int random = Global.random.Next(0, rivalGuilds.Count);

                GuildUser rivalGuild = await db.GuildUsers.FindAsync(rivalGuilds[random].Id);

                thisGuild.RivalId = rivalGuild.Id;
                rivalGuild.RivalId = thisGuild.Id;

                thisGuild.LastRivalRenewed = DateTime.Now;

                await db.SaveChangesAsync();

                await Utils.Embed
                    .SetTitle("Rival Set!")
                    .SetDescription($"Your new rival is **{rivalGuild.Name}**!")
                    .SendToChannel(context.Channel);
            }
        }

        [Command(Name = "guildprofile")]
        public async Task GuildProfile(EventContext context)
        {
            Locale locale = Locale.GetEntity(context.Channel.Id);

            using (MikiContext database = new MikiContext())
            {
                GuildUser g = await database.GuildUsers.FindAsync(context.Guild.Id.ToDbLong());

                if (g == null)
                {
                    await GuildUser.Create(context.Guild);
                    g = await database.GuildUsers.FindAsync(context.Guild.Id.ToDbLong());
                }

                int rank = g.GetGlobalRank();
                int level = g.CalculateLevel(g.Experience);

                EmojiBarSet onBarSet = new EmojiBarSet("<:mbaronright:334479818924228608>", "<:mbaronmid:334479818848468992>", "<:mbaronleft:334479819003789312>");
                EmojiBarSet offBarSet = new EmojiBarSet("<:mbaroffright:334479818714513430>", "<:mbaroffmid:334479818504536066>", "<:mbaroffleft:334479818949394442>");

                EmojiBar expBar = new EmojiBar(g.CalculateMaxExperience(g.Experience), onBarSet, offBarSet, 6);

                IDiscordEmbed embed = Utils.Embed
                    .SetAuthor(g.Name, context.Guild.AvatarUrl, "https://miki.veld.one")
                    .SetColor(0.1f, 0.6f, 1)
                    .SetThumbnailUrl("http://veld.one/assets/img/transparentfuckingimage.png")
                    .AddInlineField("Level", level.ToString())
                    .AddInlineField("Experience [" + g.Experience + "/" + g.CalculateMaxExperience(g.Experience) + "]", await expBar.Print(g.Experience, context.Channel))
                    .AddInlineField("Rank", "#" + ((rank <= 10) ? $"**{rank}**" : rank.ToString()))
                    .AddInlineField("Users", g.UserCount.ToString());

                if (g.RivalId != 0)
                {
                    GuildUser rival = await g.GetRival();
                    embed.AddInlineField("Rival", $"{rival.Name} [{rival.Experience}]");
                }

                await embed.SendToChannel(context.Channel);
            }
        }

        [Command(Name = "guildconfig")]
        public async Task SetGuildConfig(EventContext e)
        {
            using (var context = new MikiContext())
            {
                string[] arguments = e.arguments.Split(' ');
                GuildUser g = await context.GuildUsers.FindAsync(e.Guild.Id.ToDbLong());

                switch (arguments[0])
                {
                    case "expneeded":
                    {
                        if (arguments.Length > 1)
                        {
                            if (int.TryParse(arguments[1], out int value))
                            {
                                g.MinimalExperienceToGetRewards = value;
                            }
                        }
                    } break;
                    case "visible":
                    {
                        if (arguments.Length > 1)
                        {
                            if (arguments[1].ToLower() == "yes")
                            {
                                g.VisibleOnLeaderboards = true;
                            }
                            else if (arguments[1].ToLower() == "no")
                            {
                                g.VisibleOnLeaderboards = false;

                            }
                        }
                    } break;
                }
                await context.SaveChangesAsync();
            }
        }

        [Command(Name = "guildtop")]
        public async Task GuildTop(EventContext e)
        {
            using (var context = new MikiContext())
            {
                var leaderboards = context.Database.SqlQuery<LeaderboardsItem>("SELECT TOP 12 Name, Experience as Value from [dbo].GuildUsers where VisibleOnLeaderboards = 1 order by Value desc").ToList();

                IDiscordEmbed embed = Utils.Embed
                    .SetTitle("Top Guilds");

                foreach(LeaderboardsItem i in leaderboards)
                {
                    embed.AddInlineField(i.Name, i.Value.ToString());
                }

                await embed.SendToChannel(e.Channel);
            }
        }
    }
}
