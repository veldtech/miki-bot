using Microsoft.EntityFrameworkCore;
using Miki.Accounts;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Framework.Language;
using Miki.Helpers;
using Miki.Localization;
using Miki.Models;
using Miki.Models.Objects.Guild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
	[Module("Guild_Accounts")]
	internal class GuildAccountsModule
	{
		[Command(Name = "guildweekly", Aliases = new string[] { "weekly" })]
		public async Task GuildWeekly(EventContext e)
		{
			using (MikiContext database = new MikiContext())
			{
				LocalExperience thisUser = await database.LocalExperience.FindAsync(e.Guild.Id.ToDbLong(), e.Author.Id.ToDbLong());
				GuildUser thisGuild = await database.GuildUsers.FindAsync(e.Guild.Id.ToDbLong());
				Timer timer = await database.Timers.FindAsync(e.Guild.Id.ToDbLong(), e.Author.Id.ToDbLong());

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
							GuildId = e.Guild.Id.ToDbLong(),
							UserId = e.Author.Id.ToDbLong(),
							Value = DateTime.Now.AddDays(-30)
						})).Entity;
						await database.SaveChangesAsync();
					}

					if (timer.Value.AddDays(7) <= DateTime.Now)
					{
						GuildUser rival = await thisGuild.GetRival(database);

						if (rival == null)
						{
							new EmbedBuilder()
								.SetTitle(e.Locale.GetString("miki_terms_weekly"))
								.SetDescription(e.Locale.GetString("guildweekly_error_no_rival"))
								.ToEmbed().QueueToChannel(e.Channel);
							return;
						}

						if (rival.Experience > thisGuild.Experience)
						{
							new EmbedBuilder()
								.SetTitle(e.Locale.GetString("miki_terms_weekly"))
								.SetDescription(e.Locale.GetString("guildweekly_error_low_level"))
								.ToEmbed().QueueToChannel(e.Channel);
							return;
						}

						int mekosGained = (int)Math.Round((((MikiRandom.NextDouble() + 1.25) * 0.5) * 10) * thisGuild.CalculateLevel(thisGuild.Experience));

						User user = await database.Users.FindAsync(e.Author.Id.ToDbLong());

						if (user == null)
						{
							// TODO: Add response
							return;
						}

						await user.AddCurrencyAsync(mekosGained, e.Channel);

						new EmbedBuilder()
							.SetTitle(e.Locale.GetString("miki_terms_weekly"))
							.AddInlineField("Mekos", mekosGained.ToFormattedString())
							.ToEmbed().QueueToChannel(e.Channel);

						timer.Value = DateTime.Now;
						await database.SaveChangesAsync();
					}
					else
					{
						new EmbedBuilder()
							.SetTitle(e.Locale.GetString("miki_terms_weekly"))
							.SetDescription(e.Locale.GetString("guildweekly_error_timer_running", (timer.Value.AddDays(7) - DateTime.Now).ToTimeString(e.Locale)))
							.ToEmbed().QueueToChannel(e.Channel);
					}
				}
				else
				{
					new EmbedBuilder()
						.SetTitle(e.Locale.GetString("miki_terms_weekly"))
						.SetDescription(e.Locale.GetString("miki_guildweekly_insufficient_exp", thisGuild.MinimalExperienceToGetRewards.ToFormattedString()))
						.ToEmbed().QueueToChannel(e.Channel);
				}
			}
		}

		[Command(Name = "guildnewrival", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task GuildNewRival(EventContext e)
		{
			using (MikiContext context = new MikiContext())
			{
				GuildUser thisGuild = await context.GuildUsers.FindAsync(e.Guild.Id.ToDbLong());

				if (thisGuild == null)
				{
					e.ErrorEmbed(e.Locale.GetString("guild_error_null"))
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				if (thisGuild.UserCount == 0)
				{
					thisGuild.UserCount = e.Guild.MemberCount;
				}

				if (thisGuild.LastRivalRenewed.AddDays(1) > DateTime.Now)
				{
					new EmbedBuilder()
						.SetTitle(e.Locale.GetString("miki_terms_rival"))
						.SetDescription(e.Locale.GetString("guildnewrival_error_timer_running"))
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				List<GuildUser> rivalGuilds = await context.GuildUsers
					.Where((g) => Math.Abs(g.UserCount - e.Guild.MemberCount) < (g.UserCount * 0.25) && g.RivalId == 0 && g.Id != thisGuild.Id)
					.ToListAsync();

				if (rivalGuilds.Count == 0)
				{
					e.ErrorEmbed(e.Locale.GetString("guildnewrival_error_matchmaking_failed"))
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				int random = MikiRandom.Next(0, rivalGuilds.Count);

				GuildUser rivalGuild = await context.GuildUsers.FindAsync(rivalGuilds[random].Id);

				thisGuild.RivalId = rivalGuild.Id;
				rivalGuild.RivalId = thisGuild.Id;

				thisGuild.LastRivalRenewed = DateTime.Now;

				await context.SaveChangesAsync();

				new EmbedBuilder()
					.SetTitle(e.Locale.GetString("miki_terms_rival"))
					.SetDescription(e.Locale.GetString("guildnewrival_success", rivalGuild.Name))
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "guildbank")]
		public async Task GuildBankAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				GuildUser user = await context.GuildUsers.FindAsync(e.Guild.Id.ToDbLong());

				switch (e.Arguments.FirstOrDefault()?.Argument.ToLower() ?? "")
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
						await GuildBankInfoAsync(e, user);
					}
					break;
				}
			}
		}

		public async Task GuildBankInfoAsync(EventContext e, GuildUser c)
		{
			string prefix = await e.EventSystem.GetCommandHandler<SimpleCommandHandler>().GetDefaultPrefixValueAsync(e.Guild.Id);

			e.CreateEmbedBuilder()
				.WithTitle(new LanguageResource("guildbank_title", e.Guild.Name))
				.WithDescription(new LanguageResource("guildbank_info_description"))
				.WithColor(new Color(255, 255, 255))
				.WithThumbnailUrl("https://imgur.com/KXtwIWs.png")
				.AddField(
					new LanguageResource("guildbank_info_help"),
					new LanguageResource("guildbank_info_help_description", prefix),
					true
				).Build().QueueToChannel(e.Channel);
		}

		public async Task GuildBankBalance(EventContext e, MikiContext context, GuildUser c)
		{
			var account = await BankAccount.GetAsync(context, e.Author.Id, e.Guild.Id);

			e.CreateEmbedBuilder()
				.WithTitle(new LanguageResource("guildbank_title", e.Guild.Name))
				.WithColor(new Color(255, 255, 255))
				.WithThumbnailUrl("https://imgur.com/KXtwIWs.png")
				.AddField(
					new LanguageResource("guildbank_balance_title"),
					new LanguageResource("guildbank_balance", c.Currency.ToFormattedString()),
					true
				)
				.AddField(
					new LanguageResource("guildbank_contributed", "{0}"), new StringResource(account.TotalDeposited.ToFormattedString())
				).Build().QueueToChannel(e.Channel);
		}

		public async Task GuildBankDepositAsync(EventContext e, MikiContext context, GuildUser c)
		{
			int totalDeposited = e.Arguments.Get(1).TakeInt() ?? 0;

			User user = await User.GetAsync(context, e.Author.Id, e.Author.Username);

			user.RemoveCurrency(totalDeposited);
			c.Currency += totalDeposited;

			BankAccount account = await BankAccount.GetAsync(context, e.Author.Id, e.Guild.Id);
			account.Deposit(totalDeposited);

			e.CreateEmbedBuilder()
				.WithTitle("guildbank_deposit_title", e.Author.Username, totalDeposited.ToFormattedString())
				.WithColor(new Color(255, 255, 255))
				.WithThumbnailUrl("https://imgur.com/KXtwIWs.png")
				.Build().QueueToChannel(e.Channel);
		}

		[Command(Name = "guildprofile")]
		public async Task GuildProfile(EventContext e)
		{
			using (MikiContext context = new MikiContext())
			{
				GuildUser g = await context.GuildUsers.FindAsync(e.Guild.Id.ToDbLong());

				int rank = g.GetGlobalRank(context);
				int level = g.CalculateLevel(g.Experience);

				EmojiBarSet onBarSet = new EmojiBarSet("<:mbarlefton:391971424442646534>", "<:mbarmidon:391971424920797185>", "<:mbarrighton:391971424488783875>");
				EmojiBarSet offBarSet = new EmojiBarSet("<:mbarleftoff:391971424824459265>", "<:mbarmidoff:391971424824197123>", "<:mbarrightoff:391971424862208000>");

				EmojiBar expBar = new EmojiBar(g.CalculateMaxExperience(g.Experience), onBarSet, offBarSet, 6);

				EmbedBuilder embed = new EmbedBuilder()
					.SetAuthor(g.Name, e.Guild.IconUrl, "https://miki.veld.one")
					.SetColor(0.1f, 0.6f, 1)
					.SetThumbnail("http://veld.one/assets/img/transparentfuckingimage.png")
					.AddInlineField(e.Locale.GetString("miki_terms_level"), level.ToFormattedString());

				string expBarString = await expBar.Print(g.Experience, e.Guild, e.Channel as IDiscordGuildChannel);

				if (string.IsNullOrWhiteSpace(expBarString))
				{
					embed.AddInlineField(e.Locale.GetString("miki_terms_experience"), "[" + g.Experience.ToFormattedString() + " / " + g.CalculateMaxExperience(g.Experience).ToFormattedString() + "]");
				}
				else
				{
					embed.AddInlineField(e.Locale.GetString("miki_terms_experience") + $" [{g.Experience.ToFormattedString()} / {g.CalculateMaxExperience(g.Experience).ToFormattedString()}]", expBarString);
				}

				embed.AddInlineField(
					e.Locale.GetString("miki_terms_rank"), 
					"#" + ((rank <= 10) ? $"**{rank.ToFormattedString()}**" : rank.ToFormattedString())
				).AddInlineField(
					e.Locale.GetString("miki_module_general_guildinfo_users"),
					g.UserCount.ToString()
				);

				if (g.RivalId != 0)
				{
					GuildUser rival = await g.GetRival(context);
					embed.AddInlineField(e.Locale.GetString("miki_terms_rival"), $"{rival.Name} [{rival.Experience.ToFormattedString()}]");
				}

				embed.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "guildconfig", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task SetGuildConfig(EventContext e)
		{
			using (MikiContext context = new MikiContext())
			{
				GuildUser g = await context.GuildUsers.FindAsync(e.Guild.Id.ToDbLong());

				ArgObject arg = e.Arguments.FirstOrDefault();

				if (arg != null)
				{
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

									new EmbedBuilder()
										.SetTitle(e.Locale.GetString("miki_terms_config"))
										.SetDescription(e.Locale.GetString("guildconfig_expneeded", value.ToFormattedString()))
										.ToEmbed().QueueToChannel(e.Channel);
								}
							}
						}
						break;

						case "visible":
						{
							arg = arg.Next();

							if (arg != null)
							{
								bool? result = arg.TakeBoolean();

								if (!result.HasValue)
								{
									return;
								}

								string resourceString = result.Value
									? "guildconfig_visibility_true" 
									: "guildconfig_visibility_false";

								new EmbedBuilder()
									.SetTitle(e.Locale.GetString("miki_terms_config"))
									.SetDescription(resourceString)
									.ToEmbed().QueueToChannel(e.Channel);
							}
						}
						break;
					}
					await context.SaveChangesAsync();
				}
				else
				{
					new EmbedBuilder()
					{
						Title = e.Locale.GetString("guild_settings"),
						Description = e.Locale.GetString("miki_command_description_guildconfig")
					}.ToEmbed().QueueToChannel(e.Channel);
				}
			}
		}
	}
}