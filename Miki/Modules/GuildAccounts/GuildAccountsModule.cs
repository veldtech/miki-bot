using Microsoft.EntityFrameworkCore;
using Miki.Accounts;
using Miki.Attributes;
using Miki.Bot.Models;
using Miki.Bot.Models.Exceptions;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Framework.Language;
using Miki.Helpers;
using Miki.Localization;
using Miki.Models;
using Miki.Modules.GuildAccounts.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
	[Module("Guild_Accounts")]
	public class GuildAccountsModule
	{
        [GuildOnly, Command("guildweekly", "weekly")]
        public async Task GuildWeeklyAsync(IContext e)
        {
            var database = e.GetService<MikiDbContext>();

            LocalExperience thisUser = await database.LocalExperience.FindAsync(e.GetGuild().Id.ToDbLong(), e.GetAuthor().Id.ToDbLong());
            GuildUser thisGuild = await database.GuildUsers.FindAsync(e.GetGuild().Id.ToDbLong());
            Timer timer = await database.Timers.FindAsync(e.GetGuild().Id.ToDbLong(), e.GetAuthor().Id.ToDbLong());

            if (thisUser == null)
            {
                throw new UserNullException();
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
                        GuildId = e.GetGuild().Id.ToDbLong(),
                        UserId = e.GetAuthor().Id.ToDbLong(),
                        Value = DateTime.Now.AddDays(-30)
                    })).Entity;
                    await database.SaveChangesAsync();
                }

                if (timer.Value.AddDays(7) <= DateTime.Now)
                {
                    GuildUser rival = await thisGuild.GetRivalOrDefaultAsync(database);
                    if(rival == null)
                    {
                        throw new RivalNullException();
                    }

                    if (rival.Experience > thisGuild.Experience)
                    {
                        await new EmbedBuilder()
                            .SetTitle(e.GetLocale().GetString("miki_terms_weekly"))
                            .SetDescription(e.GetLocale().GetString("guildweekly_error_low_level"))
                            .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                        return;
                    }

                    int mekosGained = (int)Math.Round((((MikiRandom.NextDouble() + thisGuild.GuildHouseMultiplier) * 0.5) * 10) * thisGuild.CalculateLevel(thisGuild.Experience));

                    User user = await database.Users.FindAsync(e.GetAuthor().Id.ToDbLong());

                    if (user == null)
                    {
                        throw new UserNullException();
                    }

                    await user.AddCurrencyAsync(mekosGained, e.GetChannel());

                    await new EmbedBuilder()
                        .SetTitle(e.GetLocale().GetString("miki_terms_weekly"))
                        .AddInlineField("Mekos", mekosGained.ToFormattedString())
                        .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);

                    timer.Value = DateTime.Now;
                    await database.SaveChangesAsync();
                }
                else
                {
                    await new EmbedBuilder()
                        .SetTitle(e.GetLocale().GetString("miki_terms_weekly"))
                        .SetDescription(e.GetLocale().GetString("guildweekly_error_timer_running", (timer.Value.AddDays(7) - DateTime.Now).ToTimeString(e.GetLocale())))
                        .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                }
            }
            else
            {
                await new EmbedBuilder()
                    .SetTitle(e.GetLocale().GetString("miki_terms_weekly"))
                    .SetDescription(e.GetLocale().GetString("miki_guildweekly_insufficient_exp", thisGuild.MinimalExperienceToGetRewards.ToFormattedString()))
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
            }
        }

        [GuildOnly, Command("guildnewrival")]
        public async Task GuildNewRival(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            GuildUser thisGuild = await context.GuildUsers.FindAsync(e.GetGuild().Id.ToDbLong());

            if (thisGuild == null)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("guild_error_null"))
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                return;
            }

            if (thisGuild.UserCount == 0)
            {
                thisGuild.UserCount = e.GetGuild().MemberCount;
            }

            if (thisGuild.LastRivalRenewed.AddDays(1) > DateTime.Now)
            {
                await new EmbedBuilder()
                    .SetTitle(e.GetLocale().GetString("miki_terms_rival"))
                    .SetDescription(e.GetLocale().GetString("guildnewrival_error_timer_running"))
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                return;
            }

            List<GuildUser> rivalGuilds = await context.GuildUsers
                .Where((g) => Math.Abs(g.UserCount - e.GetGuild().MemberCount) < (g.UserCount * 0.25) && g.RivalId == 0 && g.Id != thisGuild.Id)
                .ToListAsync();

            if (rivalGuilds.Count == 0)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("guildnewrival_error_matchmaking_failed"))
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                return;
            }

            int random = MikiRandom.Next(0, rivalGuilds.Count);

            GuildUser rivalGuild = await context.GuildUsers.FindAsync(rivalGuilds[random].Id);

            thisGuild.RivalId = rivalGuild.Id;
            rivalGuild.RivalId = thisGuild.Id;

            thisGuild.LastRivalRenewed = DateTime.Now;

            await context.SaveChangesAsync();

            await new EmbedBuilder()
                .SetTitle(e.GetLocale().GetString("miki_terms_rival"))
                .SetDescription(e.GetLocale().GetString("guildnewrival_success", rivalGuild.Name))
                .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
        }

        [GuildOnly, Command("guildbank")]
        public async Task GuildBankAsync(IContext e)
        {
            e.GetArgumentPack().Take(out string arg);
            var context = e.GetService<MikiDbContext>();

            GuildUser user = await context.GuildUsers.FindAsync(e.GetGuild().Id.ToDbLong());

            switch (arg)
            {
                case "bal":
                case "balance":
                {
                    await GuildBankBalance(e, context, user);
                }
                break;

                case "deposit":
                {
                    await GuildBankDepositAsync(e, context, user);
                    await context.SaveChangesAsync();
                }
                break;

                default:
                {
                    await GuildBankInfoAsync(e);
                }
                break;
            }
        }

        public async Task GuildBankInfoAsync(IContext e)
            => await new LocalizedEmbedBuilder(e.GetLocale())
                .WithTitle(new LanguageResource("guildbank_title", e.GetGuild().Name))
                .WithDescription(new LanguageResource("guildbank_info_description"))
                .WithColor(new Color(255, 255, 255))
                .WithThumbnailUrl("https://imgur.com/KXtwIWs.png")
                .AddField(
                    new LanguageResource("guildbank_info_help"),
                    new LanguageResource("guildbank_info_help_description", e.GetPrefixMatch()),
                    true
                ).Build().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);

		public async Task GuildBankBalance(IContext e, MikiDbContext context, GuildUser c)
		{
			var account = await BankAccount.GetAsync(context, e.GetAuthor().Id, e.GetGuild().Id);

            await new LocalizedEmbedBuilder(e.GetLocale())
				.WithTitle(new LanguageResource("guildbank_title", e.GetGuild().Name))
				.WithColor(new Color(255, 255, 255))
				.WithThumbnailUrl("https://imgur.com/KXtwIWs.png")
				.AddField(
					new LanguageResource("guildbank_balance_title"),
					new LanguageResource("guildbank_balance", c.Currency.ToFormattedString()),
					true
				)
				.AddField(
					new LanguageResource("guildbank_contributed", "{0}"), new StringResource(account.TotalDeposited.ToFormattedString())
				).Build().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
		}

		public async Task GuildBankDepositAsync(IContext e, MikiDbContext context, GuildUser c)
		{
            if(!e.GetArgumentPack().Take(out int totalDeposited))
            {
                // TODO: No mekos deposit error
                return;
            }

			User user = await User.GetAsync(context, e.GetAuthor().Id, e.GetAuthor().Username);

			user.RemoveCurrency(totalDeposited);
			c.Currency += totalDeposited;

			BankAccount account = await BankAccount.GetAsync(context, e.GetAuthor().Id, e.GetGuild().Id);
			account.Deposit(totalDeposited);

            await new EmbedBuilder()
				.SetAuthor("Guild bank", "https://imgur.com/KXtwIWs.png")
				.SetDescription(e.GetLocale().GetString("guildbank_deposit_title", e.GetAuthor().Username, totalDeposited.ToFormattedString()))
				.SetColor(new Color(255, 255, 255))
				.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
		}

		[GuildOnly, Command("guildprofile")]
		public async Task GuildProfile(IContext e)
		{
            var context = e.GetService<MikiDbContext>();

            GuildUser g = await context.GuildUsers.FindAsync(e.GetGuild().Id.ToDbLong());

				int rank = await g.GetGlobalRankAsync(context);
				int level = g.CalculateLevel(g.Experience);

				EmojiBarSet onBarSet = new EmojiBarSet("<:mbarlefton:391971424442646534>", "<:mbarmidon:391971424920797185>", "<:mbarrighton:391971424488783875>");
				EmojiBarSet offBarSet = new EmojiBarSet("<:mbarleftoff:391971424824459265>", "<:mbarmidoff:391971424824197123>", "<:mbarrightoff:391971424862208000>");

				EmojiBar expBar = new EmojiBar(g.CalculateMaxExperience(g.Experience), onBarSet, offBarSet, 6);

				EmbedBuilder embed = new EmbedBuilder()
					.SetAuthor(g.Name, e.GetGuild().IconUrl, "https://miki.veld.one")
					.SetColor(0.1f, 0.6f, 1)
					.AddInlineField(e.GetLocale().GetString("miki_terms_level"), level.ToFormattedString());

				if((e.GetGuild().IconUrl ?? "") != "")
				{
					embed.SetThumbnail("http://veld.one/assets/img/transparentfuckingimage.png");
				}

				string expBarString = await expBar.Print(g.Experience, e.GetGuild(), e.GetChannel() as IDiscordGuildChannel);

				if (string.IsNullOrWhiteSpace(expBarString))
				{
					embed.AddInlineField(e.GetLocale().GetString("miki_terms_experience"), "[" + g.Experience.ToFormattedString() + " / " + g.CalculateMaxExperience(g.Experience).ToFormattedString() + "]");
				}
				else
				{
					embed.AddInlineField(e.GetLocale().GetString("miki_terms_experience") + $" [{g.Experience.ToFormattedString()} / {g.CalculateMaxExperience(g.Experience).ToFormattedString()}]", expBarString);
				}

				embed.AddInlineField(
					e.GetLocale().GetString("miki_terms_rank"), 
					"#" + ((rank <= 10) ? $"**{rank.ToFormattedString()}**" : rank.ToFormattedString())
				).AddInlineField(
					e.GetLocale().GetString("miki_module_general_guildinfo_users"),
					g.UserCount.ToString()
				);

				GuildUser rival = await g.GetRivalOrDefaultAsync(context);

				if(rival != null)
				{
					embed.AddInlineField(e.GetLocale().GetString("miki_terms_rival"), $"{rival.Name} [{rival.Experience.ToFormattedString()}]");
				}

                await embed.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
		}

		[GuildOnly, Command("guildconfig")]
		public async Task SetGuildConfig(IContext e)
		{
            var context = e.GetService<MikiDbContext>();

            GuildUser g = await context.GuildUsers.FindAsync(e.GetGuild().Id.ToDbLong());

				if (e.GetArgumentPack().Take(out string arg))
				{
					switch (arg)
					{
						case "expneeded":
						{
							if (arg != null)
							{
								if (e.GetArgumentPack().Take(out int value))
								{
									g.MinimalExperienceToGetRewards = value;

                                    await new EmbedBuilder()
										.SetTitle(e.GetLocale().GetString("miki_terms_config"))
										.SetDescription(e.GetLocale().GetString("guildconfig_expneeded", value.ToFormattedString()))
										.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
								}
							}
						}
						break;

						case "visible":
						{
                            if (e.GetArgumentPack().Take(out bool value))
                            {
                                string resourceString = value
                                    ? "guildconfig_visibility_true"
                                    : "guildconfig_visibility_false";

                                await new EmbedBuilder()
                                    .SetTitle(e.GetLocale().GetString("miki_terms_config"))
                                    .SetDescription(resourceString)
                                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                            }
						}
						break;
					}
					await context.SaveChangesAsync();
				}
				else
				{
                    await new EmbedBuilder()
					{
						Title = e.GetLocale().GetString("guild_settings"),
						Description = e.GetLocale().GetString("miki_command_description_guildconfig")
					}.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
				}
		}

		[GuildOnly, Command("guildupgrade")]
		public async Task GuildUpgradeAsync(IContext e)
		{
            e.GetArgumentPack().Take(out string arg);
            var context = e.GetService<MikiDbContext>();

            var guildUser = await context.GuildUsers
					.FindAsync(e.GetGuild().Id.ToDbLong());

				switch (arg)
				{
					case "house":
					{
						guildUser.RemoveCurrency(guildUser.GuildHouseUpgradePrice);
						guildUser.GuildHouseLevel++;

						await context.SaveChangesAsync();

                        await e.SuccessEmbed("Upgraded your guild house!")
							.QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
					} break;

					default:
					{
                        await new EmbedBuilder()
							.SetTitle("Guild Upgrades")
							.SetDescription("Guild upgrades are a list of things you can upgrade for your guild to get more rewards! To purchase one of the upgrades, use `>guildupgrade <upgrade name>` an example would be `>guildupgrade house`")
							.AddField("Upgrades",
								$"`house` - Upgrades weekly rewards (costs: {guildUser.GuildHouseUpgradePrice.ToFormattedString()})")
							.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
					} break;
				}
		}

		[GuildOnly, Command("guildhouse")]
		public async Task GuildHouseAsync(IContext e)
		{
            var context = e.GetService<MikiDbContext>();

            var guildUser = await context.GuildUsers
					.FindAsync(e.GetGuild().Id.ToDbLong());

                await new EmbedBuilder()
					.SetTitle("🏠 Guild house")
					.SetColor(255, 232, 182)
					.SetDescription(e.GetLocale().GetString("guildhouse_buy", guildUser.GuildHouseUpgradePrice.ToFormattedString()))
					.AddInlineField("Current weekly bonus", $"x{guildUser.GuildHouseMultiplier}")
					.AddInlineField("Current house level", e.GetLocale().GetString($"guildhouse_rank_{guildUser.GuildHouseLevel}"))
					.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
		}
	}
}