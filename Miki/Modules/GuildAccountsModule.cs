using Discord.WebSocket;
using Miki.Framework;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Miki.Common.Events;
using Miki.Common.Interfaces;
using Miki.Accounts;
using Miki.Languages;
using Miki.Models;
using Miki.Models.Objects.Guild;
using Miki.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Miki.Modules
{
    [Module("Guild Accounts")]
    internal class GuildAccountsModule
    {
        [Command(Name = "guildweekly", Aliases = new string[] { "weekly" })]
        public async Task GuildWeekly(EventContext context)
        {
            using (MikiContext database = new MikiContext())
            {
				Locale locale = new Locale(context.Channel.Id);
				LocalExperience thisUser = await database.LocalExperience.FindAsync(context.Guild.Id.ToDbLong(), context.Author.Id.ToDbLong());
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

                if (thisUser.Experience >= thisGuild.MinimalExperienceToGetRewards)
                {
                    if (timer == null)
                    {
                        timer = (await database.Timers.AddAsync(new Timer()
                        {
                            GuildId = context.Guild.Id.ToDbLong(),
                            UserId = context.Author.Id.ToDbLong(),
                            Value = DateTime.Now.AddDays(-30)
                        })).Entity;
                        await database.SaveChangesAsync();
                    }

                    if (timer.Value.AddDays(7) <= DateTime.Now)
                    {
                        IDiscordGuild guild = Bot.Instance.GetGuild(thisGuild.Id.FromDbLong());

                        GuildUser rival = await thisGuild.GetRival();

                        if (rival == null)
                        {
                            Utils.Embed
                                .SetTitle(locale.GetString("miki_terms_weekly"))
                                .SetDescription(context.GetResource("guildweekly_error_no_rival"))
                                .QueueToChannel(context.Channel);
                            return;
                        }

                        if (rival.Experience > thisGuild.Experience)
                        {
                            Utils.Embed
                                .SetTitle(locale.GetString("miki_terms_weekly"))
                                .SetDescription(context.GetResource("guildweekly_error_low_level"))
                                .QueueToChannel(context.Channel);
                            return;
                        }

                        int mekosGained = (int)Math.Round((((MikiRandom.NextDouble() + 1.25) * 0.5) * 10) * thisGuild.CalculateLevel(thisGuild.Experience));

                        User user = await database.Users.FindAsync(context.Author.Id.ToDbLong());

                        if (user == null)
                        {
                            // TODO: Add response
                            return;
                        }

                        await user.AddCurrencyAsync(mekosGained, context.Channel);

                        Utils.Embed
                            .SetTitle(locale.GetString("miki_terms_weekly"))
                            .AddInlineField("Mekos", mekosGained.ToString())
                            .QueueToChannel(context.Channel);

                        timer.Value = DateTime.Now;
                        await database.SaveChangesAsync();
                    }
                    else
                    {
                        Utils.Embed
                            .SetTitle(locale.GetString("miki_terms_weekly"))
                            .SetDescription(context.GetResource("guildweekly_error_timer_running", (timer.Value.AddDays(7) - DateTime.Now).ToTimeString(locale)))
                            .QueueToChannel(context.Channel);
                    }
                }
                else
                {
                    Utils.Embed
                        .SetTitle(locale.GetString("miki_terms_weekly"))
                        .SetDescription(locale.GetString("miki_guildweekly_insufficient_exp", thisGuild.MinimalExperienceToGetRewards))
                        .QueueToChannel(context.Channel);
                }
            }
        }

        [Command(Name = "guildnewrival", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task GuildNewRival(EventContext context)
        {
            using (MikiContext db = new MikiContext())
            {
                GuildUser thisGuild = await db.GuildUsers.FindAsync(context.Guild.Id.ToDbLong());

                if (thisGuild == null)
                {
                    context.ErrorEmbed(context.GetResource("guild_error_null"))
                        .QueueToChannel(context.Channel);
                    return;
                }

                if (thisGuild.LastRivalRenewed.AddDays(1) > DateTime.Now)
                {
                    Utils.Embed
                       .SetTitle(context.GetResource("miki_terms_rival"))
                       .SetDescription(context.GetResource("guildnewrival_error_timer_running"))
                       .QueueToChannel(context.Channel);
                    return;
                }

                List<GuildUser> rivalGuilds = await db.GuildUsers
                    .Where((g) => Math.Abs(g.UserCount - thisGuild.UserCount) < (g.UserCount / 4) && g.RivalId == 0 && g.Id != thisGuild.Id)
                    .ToListAsync();

                if (rivalGuilds.Count == 0)
                {
                    context.ErrorEmbed(context.GetResource("guildnewrival_error_matchmaking_failed"))
                        .QueueToChannel(context.Channel);
                    return;
                }

                int random = MikiRandom.Next(0, rivalGuilds.Count);

                GuildUser rivalGuild = await db.GuildUsers.FindAsync(rivalGuilds[random].Id);

                thisGuild.RivalId = rivalGuild.Id;
                rivalGuild.RivalId = thisGuild.Id;

                thisGuild.LastRivalRenewed = DateTime.Now;

                await db.SaveChangesAsync();

                Utils.Embed
                    .SetTitle(context.GetResource("miki_terms_rival"))
                    .SetDescription(context.GetResource("guildnewrival_success", rivalGuild.Name))
                    .QueueToChannel(context.Channel);
            }
        }

        [Command(Name = "guildprofile")]
        public async Task GuildProfile(EventContext e)
        {
            using (MikiContext context = new MikiContext())
            {
                GuildUser g = await GuildUser.GetAsync(context, e.Guild);

                int rank = g.GetGlobalRank();
                int level = g.CalculateLevel(g.Experience);

				EmojiBarSet onBarSet = new EmojiBarSet("<:mbarlefton:391971424442646534>", "<:mbarmidon:391971424920797185>", "<:mbarrighton:391971424488783875>");
				EmojiBarSet offBarSet = new EmojiBarSet("<:mbarleftoff:391971424824459265>", "<:mbarmidoff:391971424824197123>", "<:mbarrightoff:391971424862208000>");

				EmojiBar expBar = new EmojiBar(g.CalculateMaxExperience(g.Experience), onBarSet, offBarSet, 6);

                IDiscordEmbed embed = Utils.Embed
                    .SetAuthor(g.Name, e.Guild.AvatarUrl, "https://miki.veld.one")
                    .SetColor(0.1f, 0.6f, 1)
                    .SetThumbnailUrl("http://veld.one/assets/img/transparentfuckingimage.png")
                    .AddInlineField(e.GetResource("miki_terms_level"), level.ToString());

                string expBarString = await expBar.Print(g.Experience, e.Channel);

                if (string.IsNullOrWhiteSpace(expBarString))
                { 
                    embed.AddInlineField(e.GetResource("miki_terms_experience"), "[" + g.Experience + " / " + g.CalculateMaxExperience(g.Experience) + "]");
                }
                else
                {
                    embed.AddInlineField(e.GetResource("miki_terms_experience") + $" [{g.Experience} / {g.CalculateMaxExperience(g.Experience)}]", expBarString);
                }

                embed.AddInlineField(e.GetResource("miki_terms_rank"), "#" + ((rank <= 10) ? $"**{rank}**" : rank.ToString()))
                    .AddInlineField(e.GetResource("miki_module_general_guildinfo_users"), g.UserCount.ToString());

                if (g.RivalId != 0)
                {
                    GuildUser rival = await g.GetRival();
                    embed.AddInlineField(e.GetResource("miki_terms_rival"), $"{rival.Name} [{rival.Experience}]");
                }

                embed.QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "guildconfig", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SetGuildConfig(EventContext e)
        {
            using (MikiContext context = new MikiContext())
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

                                    Utils.Embed
                                        .SetTitle(e.GetResource("miki_terms_config"))
                                        .SetDescription(e.GetResource("guildconfig_expneeded", value))
                                        .QueueToChannel(e.Channel);
                                }
                            }
                        }
                        break;

                    case "visible":
                        {
                            if (arguments.Length > 1)
                            {
                                g.VisibleOnLeaderboards = arguments[1].ToBool();

                                string resourceString = g.VisibleOnLeaderboards ? "guildconfig_visibility_true" : "guildconfig_visibility_false";

                                Utils.Embed
                                    .SetTitle(e.GetResource("miki_terms_config"))
                                    .SetDescription(resourceString)
                                    .QueueToChannel(e.Channel);
                            }
                        }
                        break;
                }
                await context.SaveChangesAsync();
            }
        }

        [Command(Name = "guildtop")]
        public async Task GuildTop(EventContext e)
        {
            int amountToTake = 12;
            int.TryParse(e.arguments, out int amountToSkip);

            using (var context = new MikiContext())
            {
                int totalGuilds = (int)Math.Ceiling((double) await context.GuildUsers.CountAsync() / amountToTake);

                List<GuildUser> leaderboards = await context.GuildUsers.OrderByDescending(x => x.Experience)
                                                                      .Skip(amountToSkip * amountToTake)
                                                                      .Take(amountToTake)
                                                                      .ToListAsync();

                IDiscordEmbed embed = Utils.Embed
                    .SetTitle(e.GetResource("guildtop_title"));

                foreach (GuildUser i in leaderboards)
                {
                    embed.AddInlineField(i.Name, i.Experience.ToString());
                }

                embed.SetFooter(e.GetResource("page_index", amountToSkip + 1, totalGuilds), null);
                embed.QueueToChannel(e.Channel);
            }
        }
    }
}
