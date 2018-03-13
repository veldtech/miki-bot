using Discord.WebSocket;
using Miki.Framework;
using Miki.Framework.Events.Attributes;
using Miki.Common;
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
using Miki.Framework.Events;
using Discord;
using Miki.Framework.Extension;

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
                    return;
                }

                if (thisGuild == null)
                {
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
                        IGuild guild = Bot.Instance.Client.GetGuild(thisGuild.Id.FromDbLong());

                        GuildUser rival = await thisGuild.GetRival();

                        if (rival == null)
                        {
                            Utils.Embed
                                .WithTitle(locale.GetString("miki_terms_weekly"))
                                .WithDescription(context.GetResource("guildweekly_error_no_rival"))
								.Build().QueueToChannel(context.Channel);
                            return;
                        }

                        if (rival.Experience > thisGuild.Experience)
                        {
                            Utils.Embed
                                .WithTitle(locale.GetString("miki_terms_weekly"))
                                .WithDescription(context.GetResource("guildweekly_error_low_level"))
								.Build().QueueToChannel(context.Channel);
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
                            .WithTitle(locale.GetString("miki_terms_weekly"))
                            .AddInlineField("Mekos", mekosGained.ToString())
							.Build().QueueToChannel(context.Channel);

                        timer.Value = DateTime.Now;
                        await database.SaveChangesAsync();
                    }
                    else
                    {
                        Utils.Embed
                            .WithTitle(locale.GetString("miki_terms_weekly"))
                            .WithDescription(context.GetResource("guildweekly_error_timer_running", (timer.Value.AddDays(7) - DateTime.Now).ToTimeString(locale)))
							.Build().QueueToChannel(context.Channel);
                    }
                }
                else
                {
                    Utils.Embed
                        .WithTitle(locale.GetString("miki_terms_weekly"))
                        .WithDescription(locale.GetString("miki_guildweekly_insufficient_exp", thisGuild.MinimalExperienceToGetRewards))
						.Build().QueueToChannel(context.Channel);
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
						.Build().QueueToChannel(context.Channel);
                    return;
                }

                if (thisGuild.LastRivalRenewed.AddDays(1) > DateTime.Now)
                {
                    Utils.Embed
                       .WithTitle(context.GetResource("miki_terms_rival"))
                       .WithDescription(context.GetResource("guildnewrival_error_timer_running"))
					   .Build().QueueToChannel(context.Channel);
                    return;
                }

                List<GuildUser> rivalGuilds = await db.GuildUsers
                    .Where((g) => Math.Abs(g.UserCount - thisGuild.UserCount) < (g.UserCount / 4) && g.RivalId == 0 && g.Id != thisGuild.Id)
                    .ToListAsync();

                if (rivalGuilds.Count == 0)
                {
                    context.ErrorEmbed(context.GetResource("guildnewrival_error_matchmaking_failed"))
						.Build().QueueToChannel(context.Channel);
                    return;
                }

                int random = MikiRandom.Next(0, rivalGuilds.Count);

                GuildUser rivalGuild = await db.GuildUsers.FindAsync(rivalGuilds[random].Id);

                thisGuild.RivalId = rivalGuild.Id;
                rivalGuild.RivalId = thisGuild.Id;

                thisGuild.LastRivalRenewed = DateTime.Now;

                await db.SaveChangesAsync();

                Utils.Embed
                    .WithTitle(context.GetResource("miki_terms_rival"))
                    .WithDescription(context.GetResource("guildnewrival_success", rivalGuild.Name))
					.Build().QueueToChannel(context.Channel);
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

                EmbedBuilder embed = Utils.Embed
                    .WithAuthor(g.Name, e.Guild.IconUrl, "https://miki.veld.one")
                    .WithColor(0.1f, 0.6f, 1)
                    .WithThumbnailUrl("http://veld.one/assets/img/transparentfuckingimage.png")
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

                embed.Build().QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "guildconfig", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SetGuildConfig(EventContext e)
        {
            using (MikiContext context = new MikiContext())
            {
                GuildUser g = await context.GuildUsers.FindAsync(e.Guild.Id.ToDbLong());

				ArgObject arg = e.Arguments.FirstOrDefault();

				if(arg == null)
				{
					// TODO: error message
					return;
				}

				switch (arg.Argument)
				{
					case "expneeded":
					{
						arg = arg.Next();

						if (arg != null)
						{
							if (int.TryParse(arg.Argument, out int value))
							{
								g.MinimalExperienceToGetRewards = value;

								Utils.Embed
									.WithTitle(e.GetResource("miki_terms_config"))
									.WithDescription(e.GetResource("guildconfig_expneeded", value))
									.Build().QueueToChannel(e.Channel);
							}
						}
					}
					break;

					case "visible":
					{
						arg = arg.Next();

						if (arg != null)
						{
							bool? result = arg.AsBoolean();

							if (!result.HasValue)
								return;

							g.VisibleOnLeaderboards = result.Value;

							string resourceString = g.VisibleOnLeaderboards ? "guildconfig_visibility_true" : "guildconfig_visibility_false";

							Utils.Embed
								.WithTitle(e.GetResource("miki_terms_config"))
								.WithDescription(resourceString)
								.Build().QueueToChannel(e.Channel);
						}
					}
					break;
				}
                await context.SaveChangesAsync();
            }
        }
    }
}
