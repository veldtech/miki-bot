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
using Rest;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

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

                if (thisUser.Experience >= thisGuild.MinimalExperienceToGetRewards)
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
                                .SetDescription(context.GetResource("guildweekly_error_no_rival"))
                                .SendToChannel(context.Channel);
                            return;
                        }

                        if (rival.Experience > thisGuild.Experience)
                        {
                            await Utils.Embed
                                .SetTitle(locale.GetString("miki_terms_weekly"))
                                .SetDescription(context.GetResource("guildweekly_error_low_level"))
                                .SendToChannel(context.Channel);
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
                            .SetDescription(context.GetResource("guildweekly_error_timer_running", (timer.Value.AddDays(7) - DateTime.Now).ToTimeString(locale)))
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

                if (thisGuild == null)
                {
                    await context.ErrorEmbed(context.GetResource("guild_error_null"))
                        .SendToChannel(context.Channel);
                    return;
                }

                if (thisGuild.LastRivalRenewed.AddDays(1) > DateTime.Now)
                {
                    await Utils.Embed
                       .SetTitle(context.GetResource("miki_terms_rival"))
                       .SetDescription(context.GetResource("guildnewrival_error_timer_running"))
                       .SendToChannel(context.Channel);
                    return;
                }

                List<GuildUser> rivalGuilds = await db.GuildUsers
                    .Where((g) => Math.Abs(g.UserCount - thisGuild.UserCount) < (g.UserCount / 4) && g.RivalId == 0 && g.Id != thisGuild.Id)
                    .ToListAsync();

                if (rivalGuilds.Count == 0)
                {
                    await context.ErrorEmbed(context.GetResource("guildnewrival_error_matchmaking_failed"))
                        .SendToChannel(context.Channel);
                    return;
                }

                int random = MikiRandom.Next(0, rivalGuilds.Count);

                GuildUser rivalGuild = await db.GuildUsers.FindAsync(rivalGuilds[random].Id);

                thisGuild.RivalId = rivalGuild.Id;
                rivalGuild.RivalId = thisGuild.Id;

                thisGuild.LastRivalRenewed = DateTime.Now;

                await db.SaveChangesAsync();

                await Utils.Embed
                    .SetTitle(context.GetResource("miki_terms_rival"))
                    .SetDescription(context.GetResource("guildnewrival_success", rivalGuild.Name))
                    .SendToChannel(context.Channel);
            }
        }

        [Command(Name = "guildprofile")]
        public async Task GuildProfile(EventContext context)
        {
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
                    .AddInlineField(context.GetResource("miki_terms_level"), level.ToString());

                string expBarString = await expBar.Print(g.Experience, context.Channel);

                if (string.IsNullOrWhiteSpace(expBarString))
                { 
                    embed.AddInlineField(context.GetResource("miki_terms_experience"), "[" + g.Experience + " / " + g.CalculateMaxExperience(g.Experience) + "]");
                }
                else
                {
                    embed.AddInlineField(context.GetResource("miki_terms_experience") + $" [{g.Experience} / {g.CalculateMaxExperience(g.Experience)}]", expBarString);
                }

                embed.AddInlineField(context.GetResource("miki_terms_rank"), "#" + ((rank <= 10) ? $"**{rank}**" : rank.ToString()))
                    .AddInlineField(context.GetResource("miki_module_general_guildinfo_users"), g.UserCount.ToString());

                if (g.RivalId != 0)
                {
                    GuildUser rival = await g.GetRival();
                    embed.AddInlineField(context.GetResource("miki_terms_rival"), $"{rival.Name} [{rival.Experience}]");
                }

                await embed.SendToChannel(context.Channel);
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

                                    await Utils.Embed
                                        .SetTitle(e.GetResource("miki_terms_config"))
                                        .SetDescription(e.GetResource("guildconfig_expneeded", value))
                                        .SendToChannel(e.Channel);
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

                                await Utils.Embed
                                    .SetTitle(e.GetResource("miki_terms_config"))
                                    .SetDescription(resourceString)
                                    .SendToChannel(e.Channel);
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
			bool usePagedRoute = false;
			if (int.TryParse(e.arguments, out int amountToSkip))
			{
				usePagedRoute = true;
			}

			RestClient rest = new RestClient
				(Global.config.MikiApiBaseUrl +
				"/leaderboards/global/guilds" +
				(usePagedRoute ? $"/{amountToSkip}" : "" +
				"?key=" + Global.config.MikiApiKey));

			IDiscordEmbed embed = Utils.Embed
				.SetTitle(e.GetResource("guildtop_title"));

			List<LeaderboardsItem> items = (await rest.GetAsync<List<LeaderboardsItem>>()).Data;

			embed = Utils.RenderLeaderboards(embed, items);
			/*embed.SetFooter(e.GetResource("page_index", amountToSkip, totalGuilds), null);*/
			await embed.SendToChannel(e.Channel);
		}
    }
}
