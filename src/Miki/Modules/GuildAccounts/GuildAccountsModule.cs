namespace Miki.Modules.GuildAccounts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Attributes;
    using Bot.Models;
    using Bot.Models.Exceptions;
    using Discord;
    using Discord.Rest;
    using Framework;
    using Framework.Commands;
    using Framework.Language;
    using Helpers;
    using Localization;
    using Localization.Models;
    using Microsoft.EntityFrameworkCore;
    using Miki.Accounts;
    using Miki.Services;
    using Miki.Services.Transactions;
    using Miki.Utility;

    [Module("Guild_Accounts")]
	public class GuildAccountsModule
	{
        [GuildOnly, Command("guildweekly", "weekly")]
        public async Task GuildWeeklyAsync(IContext e)
        {
            var database = e.GetService<MikiDbContext>();
            var transactionService = e.GetService<ITransactionService>();
            var guildService = e.GetService<GuildService>();
            var locale = e.GetLocale();

            var unit = e.GetService<IUnitOfWork>();
            var localExperienceRepository = unit.GetRepository<LocalExperience>();
            var timerRepository = unit.GetRepository<Timer>();


            LocalExperience thisUser = await localExperienceRepository.GetAsync(
                (long)e.GetGuild().Id, (long)e.GetAuthor().Id);
            // TODO move to LocalExperience service?
            if(thisUser == null)
            {
                thisUser = new LocalExperience
                {
                    ServerId = (long)e.GetGuild().Id,
                    UserId = (long)e.GetAuthor().Id,
                    Experience = 0,
                };
                await localExperienceRepository.AddAsync(thisUser)
                    .ConfigureAwait(false);
                await unit.CommitAsync().ConfigureAwait(false);
            }

            GuildUser thisGuild = await guildService.GetGuildAsync((long)e.GetGuild().Id);

            if(thisUser.Experience >= thisGuild.MinimalExperienceToGetRewards)
            {
                await e.ErrorEmbedResource("miki_guildweekly_insufficient_exp",
                        thisGuild.MinimalExperienceToGetRewards.ToString("N0"))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            Timer timer = await timerRepository.GetAsync((long)e.GetGuild().Id, (long)e.GetAuthor().Id);
            if (timer == null)
            {
                timer = await timerRepository.AddAsync(
                    new Timer()
                {
                    GuildId = e.GetGuild().Id.ToDbLong(),
                    UserId = e.GetAuthor().Id.ToDbLong(),
                    Value = DateTime.Now.AddDays(-30)
                });
                await unit.CommitAsync();
            }

            if (timer.Value.AddDays(7) > DateTime.Now)
            {
                await e.ErrorEmbedResource(
                        "guildweekly_error_timer_running", 
                        (timer.Value.AddDays(7) - DateTime.Now).ToTimeString(e.GetLocale()))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            GuildUser rival = await guildService.GetRivalAsync(thisGuild);
            if (rival.Experience > thisGuild.Experience)
            {
                await e.ErrorEmbedResource("guildweekly_error_low_level")
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            // TODO: turn this into a function maybe?
            int mekosGained = (int)Math.Round(
                (MikiRandom.NextDouble() + thisGuild.GuildHouseMultiplier) 
                * 0.5 * 10 * thisGuild.CalculateLevel(thisGuild.Experience));

            await transactionService.CreateTransactionAsync(
                new TransactionRequest.Builder()
                    .WithAmount(mekosGained)
                    .WithReceiver((long)e.GetAuthor().Id)
                    .WithSender(AppProps.Currency.BankId)
                    .Build());

            await new EmbedBuilder()
                .SetTitle(locale.GetString("miki_terms_weekly"))
                .AddInlineField("Mekos", mekosGained.ToString("N0"))
                .ToEmbed().QueueAsync(e, e.GetChannel());

            timer.Value = DateTime.Now;
            await timerRepository.EditAsync(timer);
            await database.SaveChangesAsync();
        }

        [GuildOnly, Command("guildnewrival")]
        public async Task GuildNewRival(IContext e)
        {
            var context = e.GetService<MikiDbContext>();
            var locale = e.GetLocale();

            GuildUser thisGuild = await context.GuildUsers
                .FindAsync(e.GetGuild().Id.ToDbLong());

            if (thisGuild == null)
            {
                await e.ErrorEmbed(locale.GetString("guild_error_null"))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            if (thisGuild.UserCount == 0)
            {
                thisGuild.UserCount = e.GetGuild().MemberCount;
            }

            if (thisGuild.LastRivalRenewed.AddDays(1) > DateTime.Now)
            {
                await new EmbedBuilder()
                    .SetTitle(locale.GetString("miki_terms_rival"))
                    .SetDescription(locale.GetString("guildnewrival_error_timer_running"))
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }

            List<GuildUser> rivalGuilds = await context.GuildUsers
            // TODO: refactor and potentially move into function
                .Where((g) => Math.Abs(g.UserCount - e.GetGuild().MemberCount) < (g.UserCount * 0.25)
                    && g.RivalId == 0 && g.Id != thisGuild.Id)
                .ToListAsync();

            if (!rivalGuilds.Any())
            {
                await e.ErrorEmbed(locale.GetString("guildnewrival_error_matchmaking_failed"))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            int random = MikiRandom.Next(0, rivalGuilds.Count);

            GuildUser rivalGuild = await context.GuildUsers.FindAsync(rivalGuilds[random].Id);

            thisGuild.RivalId = rivalGuild.Id;
            rivalGuild.RivalId = thisGuild.Id;

            thisGuild.LastRivalRenewed = DateTime.Now;

            await context.SaveChangesAsync();

            await new EmbedBuilder()
                .SetTitle(locale.GetString("miki_terms_rival"))
                .SetDescription(locale.GetString(
                    "guildnewrival_success",
                    rivalGuild.Name))
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("guildbank")]
        [GuildOnly]
        public class GuildbankCommand
        {
            [Command]
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
                      ).Build().QueueAsync(e, e.GetChannel());

            [Command("balance", "bal")]
            public async Task GuildBankBalance(IContext e)
            {
                var context = e.GetService<DbContext>();

                var guildUser = await context.Set<GuildUser>()
                    .SingleOrDefaultAsync(x => x.Id == (long)e.GetGuild().Id);

                var account = await BankAccount.GetAsync(context, e.GetAuthor().Id, e.GetGuild().Id);

                await new LocalizedEmbedBuilder(e.GetLocale())
                    .WithTitle(new LanguageResource("guildbank_title", e.GetGuild().Name))
                    .WithColor(new Color(255, 255, 255))
                    .WithThumbnailUrl("https://imgur.com/KXtwIWs.png")
                    .AddField(
                        new LanguageResource("guildbank_balance_title"),
                        new LanguageResource("guildbank_balance", guildUser.Currency.ToString("N0")),
                        true
                    )
                    .AddField(
                        new LanguageResource("guildbank_contributed", "{0}"),
                        new StringResource(account.TotalDeposited.ToString("N0"))
                    ).Build()
                    .QueueAsync(e, e.GetChannel());
            }

            [Command("deposit", "dep")]
            public async Task GuildBankDepositAsync(IContext e)
            {
                var context = e.GetService<DbContext>();
                var userService = e.GetService<IUserService>();
                var locale = e.GetLocale();

                var guildUser = await context.Set<GuildUser>()
                    .SingleOrDefaultAsync(x => x.Id == (long)e.GetGuild().Id);

                int totalDeposited = e.GetArgumentPack().TakeRequired<int>();

                User user = await userService.GetOrCreateUserAsync(e.GetAuthor());

                user.RemoveCurrency(totalDeposited);
                guildUser.Currency += totalDeposited;

                await userService.UpdateUserAsync(user);
                // TODO: abstractify to a service
                context.Update(guildUser);

                BankAccount account = await BankAccount.GetAsync(context, e.GetAuthor().Id, e.GetGuild().Id);
                context.Update(account);
                account.Deposit(totalDeposited);
                await context.SaveChangesAsync();

                await new EmbedBuilder()
                    .SetAuthor("Guild bank", "https://imgur.com/KXtwIWs.png")
                    .SetDescription(locale.GetString("guildbank_deposit_title", e.GetAuthor().Username, totalDeposited.ToString("N0")))
                    .SetColor(new Color(255, 255, 255))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
            }
        }

		[GuildOnly, Command("guildprofile")]
		public async Task GuildProfile(IContext e)
		{
            var context = e.GetService<MikiDbContext>();

            var locale = e.GetLocale();
            GuildUser g = await context.GuildUsers.FindAsync(e.GetGuild().Id.ToDbLong());

				int rank = await g.GetGlobalRankAsync(context);
				int level = g.CalculateLevel(g.Experience);

				EmojiBarSet onBarSet = new EmojiBarSet("<:mbarlefton:391971424442646534>", "<:mbarmidon:391971424920797185>", "<:mbarrighton:391971424488783875>");
				EmojiBarSet offBarSet = new EmojiBarSet("<:mbarleftoff:391971424824459265>", "<:mbarmidoff:391971424824197123>", "<:mbarrightoff:391971424862208000>");

				EmojiBar expBar = new EmojiBar(g.CalculateMaxExperience(g.Experience), onBarSet, offBarSet, 6);

				EmbedBuilder embed = new EmbedBuilder()
					.SetAuthor(g.Name, e.GetGuild().IconUrl, "https://miki.veld.one")
					.SetColor(0.1f, 0.6f, 1)
					.AddInlineField(locale.GetString("miki_terms_level"), level.ToString("N0"));

				if((e.GetGuild().IconUrl ?? "") != "")
				{
					embed.SetThumbnail("http://veld.one/assets/img/transparentfuckingimage.png");
				}

				string expBarString = expBar.Print(g.Experience);

                embed.AddInlineField(e.GetLocale().GetString("miki_terms_experience"),
                    $"[{g.Experience:N0} / {g.CalculateMaxExperience(g.Experience):N0}]\n" 
                    + (expBarString ?? ""));

				embed.AddInlineField(
					e.GetLocale().GetString("miki_terms_rank"), 
					"#" + (rank <= 10 ? $"**{rank:N0}**" : rank.ToString("N0"))
				).AddInlineField(
					e.GetLocale().GetString("miki_module_general_guildinfo_users"),
					g.UserCount.ToString()
				);

				GuildUser rival = await g.GetRivalOrDefaultAsync(context);

				if(rival != null)
				{
					embed.AddInlineField(e.GetLocale().GetString("miki_terms_rival"), $"{rival.Name} [{rival.Experience:N0}]");
				}

                await embed.ToEmbed()
                    .QueueAsync(e, e.GetChannel());
		}

        [GuildOnly, Command("guildconfig")]
        public async Task SetGuildConfig(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            GuildUser g = await context.GuildUsers.FindAsync(e.GetGuild().Id.ToDbLong());

            if(e.GetArgumentPack().Take(out string arg))
            {
                switch(arg)
                {
                    case "expneeded":
                    {
                        if(e.GetArgumentPack().Take(out int value))
                        {
                            g.MinimalExperienceToGetRewards = value;

                            await new EmbedBuilder()
                                .SetTitle(e.GetLocale().GetString("miki_terms_config"))
                                .SetDescription(e.GetLocale().GetString("guildconfig_expneeded",
                                    value.ToString("N0")))
                                .ToEmbed()
                                .QueueAsync(e, e.GetChannel());
                        }
                    }
                        break;

                    case "visible":
                    {
                        if(e.GetArgumentPack().Take(out bool value))
                        {
                            string resourceString = value
                                ? "guildconfig_visibility_true"
                                : "guildconfig_visibility_false";

                            await new EmbedBuilder()
                                .SetTitle(e.GetLocale().GetString("miki_terms_config"))
                                .SetDescription(resourceString)
                                .ToEmbed().QueueAsync(e, e.GetChannel());
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
                }.ToEmbed().QueueAsync(e, e.GetChannel());
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
							.QueueAsync(e, e.GetChannel());
					} break;

					default:
					{
                        await new EmbedBuilder()
							.SetTitle("Guild Upgrades")
							.SetDescription("Guild upgrades are a list of things you can upgrade for your guild to get more rewards! To purchase one of the upgrades, use `>guildupgrade <upgrade name>` an example would be `>guildupgrade house`")
							.AddField("Upgrades",
								$"`house` - Upgrades weekly rewards (costs: {guildUser.GuildHouseUpgradePrice:N0})")
							.ToEmbed().QueueAsync(e, e.GetChannel());
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
					.SetDescription(e.GetLocale().GetString("guildhouse_buy", guildUser.GuildHouseUpgradePrice.ToString("N0")))
					.AddInlineField("Current weekly bonus", $"x{guildUser.GuildHouseMultiplier}")
					.AddInlineField("Current house level", e.GetLocale().GetString($"guildhouse_rank_{guildUser.GuildHouseLevel}"))
					.ToEmbed().QueueAsync(e, e.GetChannel());
		}
	}
}