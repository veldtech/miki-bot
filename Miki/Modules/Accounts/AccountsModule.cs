#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Microsoft.EntityFrameworkCore;
using Miki.Accounts;
using Miki.Accounts.Achievements;
using Miki.API;
using Miki.API.Leaderboards;
using Miki.Bot.Models;
using Miki.Bot.Models.Exceptions;
using Miki.Bot.Models.Repositories;
using Miki.Cache;
using Miki.Common.Builders;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Exceptions;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Helpers;
using Miki.Logging;
using Miki.Models;
using Miki.Models.Objects.Backgrounds;
using Miki.Modules.Accounts.Services;
using Miki.Rest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miki.Modules.AccountsModule
{
	[Module("Accounts")]
	public class AccountsModule
	{
		//[Service("experience")]
		//public ExperienceTrackerService ExperienceService { get; set; }

		//[Service("achievements")]
		//public AchievementsService AchievementsService { get; set; }

        private readonly RestClient client;

		private readonly EmojiBarSet onBarSet = new EmojiBarSet(
			"<:mbarlefton:391971424442646534>",
			"<:mbarmidon:391971424920797185>",
			"<:mbarrighton:391971424488783875>");

		private readonly EmojiBarSet offBarSet = new EmojiBarSet(
			"<:mbarleftoff:391971424824459265>",
			"<:mbarmidoff:391971424824197123>",
			"<:mbarrightoff:391971424862208000>");

        public AccountsModule()
        {
            if(!string.IsNullOrWhiteSpace(Global.Config.MikiApiKey) 
                && !string.IsNullOrWhiteSpace(Global.Config.ImageApiUrl))
            {
                client = new RestClient(Global.Config.ImageApiUrl)
                    .AddHeader("Authorization", Global.Config.MikiApiKey);
            }
            else
            {
                Log.Warning("Image API can not be loaded in AccountsModule");
            }
        }

		[Command("achievements")]
		public async Task AchievementsAsync(IContext e)
		{
            var context = e.GetService<MikiDbContext>();

            long id = (long)e.GetAuthor().Id;

                if (e.GetArgumentPack().Take(out string arg))
				{
					IDiscordUser user = await DiscordExtensions.GetUserAsync(arg, e.GetGuild());

					if (user != null)
					{
						id = (long)user.Id;
					}
				}

				IDiscordUser discordUser = await e.GetGuild().GetMemberAsync(id.FromDbLong());
				User u = await User.GetAsync(context, discordUser.Id, discordUser.Username);

				List<Achievement> achievements = await context.Achievements
					.Where(x => x.UserId == id)
					.ToListAsync();

				EmbedBuilder embed = new EmbedBuilder()
					.SetAuthor($"{u.Name} | " + "Achievements", discordUser.GetAvatarUrl(), "https://miki.ai/profiles/ID/achievements");

				embed.SetColor(255, 255, 255);

				StringBuilder leftBuilder = new StringBuilder();

				int totalScore = 0;

				foreach (var a in achievements)
				{
					IAchievement metadata = AchievementManager.Instance.GetContainerById(a.Name).Achievements[a.Rank];
					leftBuilder.AppendLine(metadata.Icon + " | `" + metadata.Name.PadRight(15) + $"{metadata.Points.ToString().PadLeft(3)} pts` | 📅 {a.UnlockedAt.ToShortDateString()}");
					totalScore += metadata.Points;
				}

				if (string.IsNullOrEmpty(leftBuilder.ToString()))
				{
					embed.AddInlineField("Total Pts: " + totalScore.ToFormattedString(), "None, yet.");
				}
				else
				{
					embed.AddInlineField("Total Pts: " + totalScore.ToFormattedString(), leftBuilder.ToString());
				}

				await embed.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
		}

		[Command("exp")]
		public async Task ExpAsync(IContext e)
		{
			Stream s = await client.GetStreamAsync("api/user?id=" + e.GetMessage().Author.Id);
			if (s == null)
			{
				await e.ErrorEmbed("Image generation API did not respond. This is an issue, please report it.")
					.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
				return;
			}
			(e.GetChannel() as IDiscordTextChannel)
                .QueueMessage(stream: s);
		}

        [Command("leaderboards", "lb", "leaderboard", "top")]
        public async Task LeaderboardsAsync(IContext e)
        {
            LeaderboardsOptions options = new LeaderboardsOptions();

            e.GetArgumentPack().Peek(out string argument);

            switch (argument?.ToLower() ?? "")
            {
                case "commands":
                case "cmds":
                {
                    options.Type = LeaderboardsType.COMMANDS;
                    e.GetArgumentPack().Skip();
                }
                break;

                case "currency":
                case "mekos":
                case "money":
                case "bal":
                {
                    options.Type = LeaderboardsType.CURRENCY;
                    e.GetArgumentPack().Skip();
                }
                break;

                case "rep":
                case "reputation":
                {
                    options.Type = LeaderboardsType.REPUTATION;
                    e.GetArgumentPack().Skip();
                }
                break;

                case "pasta":
                case "pastas":
                {
                    options.Type = LeaderboardsType.PASTA;
                    e.GetArgumentPack().Skip();
                }
                break;

                case "experience":
                case "exp":
                {
                    options.Type = LeaderboardsType.EXPERIENCE;
                    e.GetArgumentPack().Skip();
                }
                break;

                case "guild":
                case "guilds":
                {
                    options.Type = LeaderboardsType.GUILDS;
                    e.GetArgumentPack().Skip();
                }
                break;

                default:
                {
                    options.Type = LeaderboardsType.EXPERIENCE;
                }
                break;
            }

            if (e.GetArgumentPack().Peek(out string localArg))
            {
                if (localArg.ToLower() == "local")
                {
                    if (options.Type != LeaderboardsType.PASTA)
                    {
                        options.GuildId = e.GetGuild().Id;
                    }
                    e.GetArgumentPack().Skip();
                }
            }

            if (e.GetArgumentPack().Peek(out int index))
            {
                options.Offset = Math.Max(0, index - 1) * 12;
                e.GetArgumentPack().Skip();
            }

            options.Amount = 12;

            var api = e.GetService<MikiApiClient>();

            LeaderboardsObject obj = await api.GetPagedLeaderboardsAsync(options);

            await Utils.RenderLeaderboards(new EmbedBuilder(), obj.items, obj.currentPage * 12)
                .SetFooter(
                    e.GetLocale().GetString("page_index", obj.currentPage + 1, Math.Ceiling((double)obj.totalPages / 10)),
                    ""
                )
                .SetAuthor(
                    "Leaderboards: " + options.Type + " (click me!)",
                    null,
                    api.BuildLeaderboardsUrl(options)
                )
                .ToEmbed()
                .QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
        }

        [Command("profile")]
        public async Task ProfileAsync(IContext e)
        {
            var args = e.GetArgumentPack();

            var context = e.GetService<MikiDbContext>();
            long id = 0;
            ulong uid = 0;

            IDiscordGuildUser discordUser = null;

            MarriageRepository repository = new MarriageRepository(context);

            if (args.Take(out string arg))
            {
                discordUser = await DiscordExtensions.GetUserAsync(arg, e.GetGuild());

                if (discordUser == null)
                {
                    throw new UserNullException();
                }

                uid = discordUser.Id;
                id = uid.ToDbLong();
            }
            else
            {
                uid = e.GetMessage().Author.Id;
                discordUser = await e.GetGuild().GetMemberAsync(uid);
                id = uid.ToDbLong();
            }

            User account = await User.GetAsync(context, discordUser.Id.ToDbLong(), discordUser.Username);
            if (account == null)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("error_account_null"))
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                return;
            }

            string icon = "";

            if (await account.IsDonatorAsync(context))
            {
                icon = "https://cdn.discordapp.com/emojis/421969679561785354.png";
            }

            EmbedBuilder embed = new EmbedBuilder()
                .SetDescription(account.Title)
                .SetAuthor(e.GetLocale().GetString(
                    "miki_global_profile_user_header", discordUser.Username), 
                    icon, "https://patreon.com/mikibot")
                .SetThumbnail(discordUser.GetAvatarUrl());

            LocalExperience localExp = await LocalExperience.GetAsync(context, 
                (long)e.GetGuild().Id, 
                (long)discordUser.Id, 
                discordUser.Username);

            int rank = await localExp.GetRankAsync(context);
            int localLevel = User.CalculateLevel(localExp.Experience);
            int maxLocalExp = User.CalculateLevelExperience(localLevel);
            int minLocalExp = User.CalculateLevelExperience(localLevel - 1);

            EmojiBar expBar = new EmojiBar(maxLocalExp - minLocalExp, onBarSet, offBarSet, 6);

            string infoValue = new MessageBuilder()
                .AppendText(e.GetLocale().GetString(
                    "miki_module_accounts_information_level",
                    localLevel,
                    localExp.Experience.ToFormattedString(),
                    maxLocalExp.ToFormattedString()))
                .AppendText(await expBar.Print(
                    localExp.Experience - minLocalExp,
                    e.GetGuild(),
                    e.GetChannel() as IDiscordGuildChannel))
                .AppendText(e.GetLocale().GetString(
                    "miki_module_accounts_information_rank",
                    rank.ToFormattedString()))
                .AppendText(
                    $"Reputation: {account.Reputation:N0}",
                    newLine: false)
                .Build();

            embed.AddInlineField(e.GetLocale().GetString("miki_generic_information"), infoValue);

            int globalLevel = User.CalculateLevel(account.Total_Experience);
            int maxGlobalExp = User.CalculateLevelExperience(globalLevel);
            int minGlobalExp = User.CalculateLevelExperience(globalLevel - 1);

            int? globalRank = await account.GetGlobalRankAsync(context);

            EmojiBar globalExpBar = new EmojiBar(maxGlobalExp - minGlobalExp, onBarSet, offBarSet, 6);

            string globalInfoValue = new MessageBuilder()
                .AppendText(e.GetLocale().GetString("miki_module_accounts_information_level", globalLevel.ToFormattedString(), account.Total_Experience.ToFormattedString(), maxGlobalExp.ToFormattedString()))
                .AppendText(
                    await globalExpBar.Print(account.Total_Experience - minGlobalExp, e.GetGuild(), e.GetChannel() as IDiscordGuildChannel)
                )
                .AppendText(e.GetLocale().GetString("miki_module_accounts_information_rank", globalRank?.ToFormattedString() ?? "We haven't calculated your rank yet!"), MessageFormatting.Plain, false)
                .Build();

            embed.AddInlineField(
                e.GetLocale().GetString("miki_generic_global_information"), 
                globalInfoValue);

            embed.AddInlineField(
                e.GetLocale().GetString("miki_generic_mekos"), 
                $"{account.Currency:N0} <:mekos:421972155484471296>");

            List<UserMarriedTo> Marriages = (await repository.GetMarriagesAsync(id))
                .Where(x => !x.Marriage.IsProposing)
                .ToList();

            List<string> users = new List<string>();

            int maxCount = Marriages?.Count ?? 0;

            for (int i = 0; i < maxCount; i++)
            {
                users.Add((await MikiApp.Instance.Discord
                    .GetUserAsync(Marriages[i].GetOther(id).FromDbLong())).Username);
            }

            if (Marriages?.Count > 0)
            {
                List<string> MarriageStrings = new List<string>();

                for (int i = 0; i < maxCount; i++)
                {
                    if (Marriages[i].GetOther(id) == 0)
                    {
                        continue;
                    }
                    MarriageStrings.Add(
                        $"💕 {users[i]} (_{Marriages[i].Marriage.TimeOfMarriage.ToShortDateString()}_)");
                }

                string marriageText = string.Join("\n", MarriageStrings);
                if (string.IsNullOrEmpty(marriageText))
                {
                    marriageText = e.GetLocale().GetString("miki_placeholder_null");
                }

                embed.AddInlineField(
                    e.GetLocale().GetString("miki_module_accounts_profile_marriedto"),
                    marriageText);
            }

            Random r = new Random((int)id - 3);
            Color c = new Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());

            embed.SetColor(c);

            List<Achievement> allAchievements = await context.Achievements.Where(x => x.UserId == id)
                .ToListAsync();

            string achievements = e.GetLocale().GetString("miki_placeholder_null");

            if (allAchievements != null)
            {
                if (allAchievements.Count > 0)
                {
                    achievements = AchievementManager.Instance.PrintAchievements(allAchievements);
                }
            }

            embed.AddInlineField(
                e.GetLocale().GetString("miki_generic_achievements"),
                achievements);

            embed.SetFooter(
                e.GetLocale().GetString(
                    "miki_module_accounts_profile_footer",
                    account.DateCreated.ToShortDateString(),
                    "NaN"));

            await embed.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
        }

        [Command("setbackground")]
        public async Task SetProfileBackgroundAsync(IContext e)
        {
            if (!e.GetArgumentPack().Take(out int backgroundId))
            {
                throw new ArgumentNullException("background");
            }

            long userId = e.GetAuthor().Id.ToDbLong();

            var context = e.GetService<MikiDbContext>();

            BackgroundsOwned bo = await context.BackgroundsOwned.FindAsync(userId, backgroundId);
            if (bo == null)
            {
                throw new BackgroundNotOwnedException();
            }

            ProfileVisuals v = await ProfileVisuals.GetAsync(userId, context);
            v.BackgroundId = bo.BackgroundId;
            await context.SaveChangesAsync();

            await e.SuccessEmbed("Successfully set background.")
                .QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
        }

        [Command("buybackground")]
        public async Task BuyProfileBackgroundAsync(IContext e)
        {
            var backgrounds = e.GetService<BackgroundStore>();

            if (!e.GetArgumentPack().Take(out int id))
            {
                (e.GetChannel() as IDiscordTextChannel).QueueMessage("Enter a number after `>buybackground` to check the backgrounds! (e.g. >buybackground 1)");
            }

            if (id >= backgrounds.Backgrounds.Count || id < 0)
            {
                await e.ErrorEmbed("This background does not exist!")
                    .ToEmbed()
                    .QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                return;
            }

            Background background = backgrounds.Backgrounds[id];

            var embed = new EmbedBuilder()
                .SetTitle("Buy Background")
                .SetImage(background.ImageUrl);

            if (background.Price > 0)
            {
                embed.SetDescription($"This background for your profile will cost {background.Price.ToFormattedString()} mekos, Type `>buybackground {id} yes` to buy.");
            }
            else
            {
                embed.SetDescription($"This background is not for sale.");
            }

            if (e.GetArgumentPack().Take(out string confirmation))
            {
                if (confirmation.ToLower() == "yes")
                {
                    if (background.Price > 0)
                    {
                        var context = e.GetService<MikiDbContext>();

                        User user = await User.GetAsync(context, e.GetAuthor().Id, e.GetAuthor().Username);
                        long userId = (long)e.GetAuthor().Id;

                        BackgroundsOwned bo = await context.BackgroundsOwned.FindAsync(userId, background.Id);

                        if (bo == null)
                        {
                            user.RemoveCurrency(background.Price);
                            await context.BackgroundsOwned.AddAsync(new BackgroundsOwned()
                            {
                                UserId = e.GetAuthor().Id.ToDbLong(),
                                BackgroundId = background.Id,
                            });

                            await context.SaveChangesAsync();

                            await e.SuccessEmbed("Background purchased!")
                                .QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);

                        }
                        else
                        {
                            throw new BackgroundOwnedException();
                        }
                    }
                    return;
                }
            }

            await embed.ToEmbed()
                .QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
        }

        [Command("setbackcolor")]
        public async Task SetProfileBackColorAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();
            User user = await DatabaseHelpers.GetUserAsync(context, e.GetAuthor());

            var x = Regex.Matches(e.GetArgumentPack().Pack.TakeAll().ToUpper(), "(#)?([A-F0-9]{6})");

            if (x.Count > 0)
            {
                ProfileVisuals visuals = await ProfileVisuals.GetAsync(e.GetAuthor().Id, context);
                var hex = x.First().Groups.Last().Value;

                visuals.BackgroundColor = hex;
                user.RemoveCurrency(250);
                await context.SaveChangesAsync();

                await e.SuccessEmbed($"Your foreground color has been successfully changed to `{hex}`")
                    .QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
            }
            else
            {
                await new EmbedBuilder()
                    .SetTitle("🖌 Setting a background color!")
                    .SetDescription("Changing your background color costs 250 mekos. use `>setbackcolor (e.g. #00FF00)` to purchase")
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
            }
        }

        [Command("setfrontcolor")]
        public async Task SetProfileForeColorAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            User user = await DatabaseHelpers.GetUserAsync(context, e.GetAuthor());

            var x = Regex.Matches(e.GetArgumentPack().Pack.TakeAll().ToUpper(), "(#)?([A-F0-9]{6})");

            if (x.Count > 0)
            {
                ProfileVisuals visuals = await ProfileVisuals.GetAsync(e.GetAuthor().Id, context);
                var hex = x.First().Groups.Last().Value;

                visuals.ForegroundColor = hex;
                user.RemoveCurrency(250);
                await context.SaveChangesAsync();

                await e.SuccessEmbed($"Your foreground color has been successfully changed to `{hex}`")
                    .QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
            }
            else
            {
                await new EmbedBuilder()
                    .SetTitle("🖌 Setting a foreground color!")
                    .SetDescription("Changing your foreground(text) color costs 250 mekos. use `>setfrontcolor (e.g. #00FF00)` to purchase")
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
            }
        }

		[Command("backgroundsowned")]
		public async Task BackgroundsOwnedAsync(IContext e)
		{
            var context = e.GetService<MikiDbContext>();

            List<BackgroundsOwned> backgroundsOwned = await context.BackgroundsOwned.Where(x => x.UserId == e.GetAuthor().Id.ToDbLong())
					.ToListAsync();

                await new EmbedBuilder()
					.SetTitle($"{e.GetAuthor().Username}'s backgrounds")
					.SetDescription(string.Join(",", backgroundsOwned.Select(x => $"`{x.BackgroundId}`")))
					.ToEmbed()
					.QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
		}

        [Command("rep")]
        public async Task GiveReputationAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            User giver = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());

            var cache = e.GetService<ICacheClient>();

            var repObject = await cache.GetAsync<ReputationObject>($"user:{giver.Id}:rep");

            if (repObject == null)
            {
                repObject = new ReputationObject()
                {
                    LastReputationGiven = DateTime.Now,
                    ReputationPointsLeft = 3
                };

                await cache.UpsertAsync(
                    $"user:{giver.Id}:rep",
                    repObject,
                    DateTime.UtcNow.AddDays(1).Date - DateTime.UtcNow
                );
            }

            if (!e.GetArgumentPack().CanTake)
            {
                TimeSpan pointReset = (DateTime.Now.AddDays(1).Date - DateTime.Now);

                await new EmbedBuilder()
                {
                    Title = e.GetLocale().GetString("miki_module_accounts_rep_header"),
                    Description = e.GetLocale().GetString("miki_module_accounts_rep_description")
                }.AddInlineField(e.GetLocale().GetString("miki_module_accounts_rep_total_received"), giver.Reputation.ToFormattedString())
                    .AddInlineField(e.GetLocale().GetString("miki_module_accounts_rep_reset"), pointReset.ToTimeString(e.GetLocale()).ToString())
                    .AddInlineField(e.GetLocale().GetString("miki_module_accounts_rep_remaining"), repObject.ReputationPointsLeft.ToString())
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                return;
            }
            else
            {
                Dictionary<IDiscordUser, short> usersMentioned = new Dictionary<IDiscordUser, short>();

                EmbedBuilder embed = new EmbedBuilder();

                int totalAmountGiven = 0;
                bool mentionedSelf = false;

                while (e.GetArgumentPack().CanTake && totalAmountGiven <= repObject.ReputationPointsLeft)
                {
                    short amount = 1;

                    e.GetArgumentPack().Take(out string userName);

                    var u = await DiscordExtensions.GetUserAsync(userName, e.GetGuild());

                    if (u == null)
                    {
                        throw new UserNullException();
                    }

                    if (e.GetArgumentPack().Take(out int value))
                    {
                        amount = (short)value;
                    }
                    else if (e.GetArgumentPack().Peek(out string arg))
                    {
                        if (Utils.IsAll(arg))
                        {
                            amount = (short)(repObject.ReputationPointsLeft - ((short)usersMentioned.Sum(x => x.Value)));
                            e.GetArgumentPack().Skip();
                        }
                    }

                    if (u.Id == e.GetAuthor().Id)
                    {
                        mentionedSelf = true;
                        continue;
                    }

                    totalAmountGiven += amount;

                    if (usersMentioned.Keys.Where(x => x.Id == u.Id).Count() > 0)
                    {
                        usersMentioned[usersMentioned.Keys.Where(x => x.Id == u.Id).First()] += amount;
                    }
                    else
                    {
                        usersMentioned.Add(u, amount);
                    }
                }

                if (mentionedSelf)
                {
                    embed.Footer = new EmbedFooter()
                    {
                        Text = e.GetLocale().GetString("warning_mention_self"),
                    };
                }

                if (usersMentioned.Count == 0)
                {
                    return;
                }
                else
                {
                    if (totalAmountGiven <= 0)
                    {
                        await e.ErrorEmbedResource("miki_module_accounts_rep_error_zero")
                            .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                        return;
                    }

                    if (usersMentioned.Sum(x => x.Value) > repObject.ReputationPointsLeft)
                    {
                        await e.ErrorEmbedResource("error_rep_limit", usersMentioned.Count, usersMentioned.Sum(x => x.Value), repObject.ReputationPointsLeft)
                            .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                        return;
                    }
                }

                embed.Title = (e.GetLocale().GetString("miki_module_accounts_rep_header"));
                embed.Description = (e.GetLocale().GetString("rep_success"));

                foreach (var u in usersMentioned)
                {
                    User receiver = await DatabaseHelpers.GetUserAsync(context, u.Key);

                    receiver.Reputation += u.Value;

                    embed.AddInlineField(
                        receiver.Name,
                        string.Format("{0} => {1} (+{2})", (receiver.Reputation - u.Value).ToFormattedString(), receiver.Reputation.ToFormattedString(), u.Value)
                    );
                }

                repObject.ReputationPointsLeft -= (short)usersMentioned.Sum(x => x.Value);

                await cache.UpsertAsync(
                    $"user:{giver.Id}:rep",
                    repObject,
                    DateTime.UtcNow.AddDays(1).Date - DateTime.UtcNow
                );

                await embed.AddInlineField(e.GetLocale().GetString("miki_module_accounts_rep_points_left"), repObject.ReputationPointsLeft.ToString())
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);

                await context.SaveChangesAsync();
            }
        }

        [Command("syncname")]
        public async Task SyncNameAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            User user = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());

            if (user == null)
            {
                throw new UserNullException();
            }

            user.Name = e.GetAuthor().Username;
            await context.SaveChangesAsync();

            EmbedBuilder embed = new EmbedBuilder();
            embed.Title = "👌 OKAY";
            embed.Description = e.GetLocale().GetString("sync_success", "name");
            await embed.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
        }

        [Command("mekos", "bal", "meko" )]
        public async Task ShowMekosAsync(IContext e)
        {
            IDiscordGuildUser member;

            if (e.GetArgumentPack().Take(out string value))
            {
                member = await DiscordExtensions.GetUserAsync(value, e.GetGuild());
            }
            else
            {
                member = await e.GetGuild().GetMemberAsync(e.GetAuthor().Id);
            }

            var context = e.GetService<MikiDbContext>();

            User user = await User.GetAsync(context, member.Id.ToDbLong(), member.Username);

            await new EmbedBuilder()
            {
                Title = "🔸 Mekos",
                Description = e.GetLocale().GetString("miki_user_mekos", user.Name, user.Currency.ToFormattedString()),
                Color = new Color(1f, 0.5f, 0.7f)
            }.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
            await context.SaveChangesAsync();
        }

        [Command("give")]
        public async Task GiveMekosAsync(IContext e)
        {
            if (e.GetArgumentPack().Take(out string userName))
            {
                var user = await DiscordExtensions.GetUserAsync(userName, e.GetGuild());

                if (user == null)
                {
                    await e.ErrorEmbedResource("give_error_no_mention")
                        .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                    return;
                }

                if (!e.GetArgumentPack().Take(out int amount))
                {
                    await e.ErrorEmbedResource("give_error_amount_unparsable")
                        .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                    return;
                }

                var context = e.GetService<MikiDbContext>();

                User sender = await DatabaseHelpers.GetUserAsync(context, e.GetAuthor());
                User receiver = await DatabaseHelpers.GetUserAsync(context, user);

                if (amount <= sender.Currency)
                {
                    sender.RemoveCurrency(amount);

                    if (await receiver.IsBannedAsync(context))
                    {
                        throw new UserNullException();
                    }

                    await receiver.AddCurrencyAsync(amount);

                    await new EmbedBuilder()
                    {
                        Title = "🔸 transaction",
                        Description = e.GetLocale().GetString("give_description", 
                            sender.Name, 
                            receiver.Name, 
                            amount.ToFormattedString()),
                        Color = new Color(255, 140, 0),
                    }.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                    await context.SaveChangesAsync();
                }
                else
                {
                    throw new InsufficientCurrencyException(sender.Currency, amount);
                }
            }
        }

        [Command("daily")]
        public async Task GetDailyAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            User u = await DatabaseHelpers.GetUserAsync(context, e.GetAuthor());

            if (u == null)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("user_error_no_account"))
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                return;
            }

            int dailyAmount = 100;
            int dailyStreakAmount = 20;

            if (await u.IsDonatorAsync(context))
            {
                dailyAmount *= 2;
                dailyStreakAmount *= 2;
            }

            if (u.LastDailyTime.AddHours(23) >= DateTime.Now)
            {
                var time = (u.LastDailyTime.AddHours(23) - DateTime.Now).ToTimeString(e.GetLocale());

                var builder = e.ErrorEmbed($"You already claimed your daily today! Please wait another `{time}` before using it again.");

                switch (MikiRandom.Next(2))
                {
                    case 0:
                    {
                        builder.AddInlineField("Appreciate Miki?", "Vote for us every day on [DiscordBots](https://discordbots.org/bot/160105994217586689/vote) to get an additional bonus!");
                    }
                    break;
                    case 1:
                    {
                        builder.AddInlineField("Appreciate Miki?", "Donate to us on [Patreon](https://patreon.com/mikibot) for more mekos!");
                    }
                    break;
                }
                await builder.ToEmbed()
                    .QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                return;
            }

            int streak = 0;
            string redisKey = $"user:{e.GetAuthor().Id}:daily";

            var cache = e.GetService<ICacheClient>();

            if (await cache.ExistsAsync(redisKey))
            {
                streak = await cache.GetAsync<int>(redisKey);
                streak++;
            }

            int amount = dailyAmount + (dailyStreakAmount * Math.Min(100, streak));

            await u.AddCurrencyAsync(amount);
            u.LastDailyTime = DateTime.Now;

            var embed = new EmbedBuilder()
                .SetTitle("💰 Daily")
                .SetDescription(e.GetLocale().GetString("daily_received", $"**{amount.ToFormattedString()}**", $"`{u.Currency.ToFormattedString()}`"))
                .SetColor(253, 216, 136);

            if (streak > 0)
            {
                embed.AddInlineField("Streak!", $"You're on a {streak.ToFormattedString()} day daily streak!");
            }

            await embed.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);

            await cache.UpsertAsync(redisKey, streak, new TimeSpan(48, 0, 0));
            await context.SaveChangesAsync();
        }
	}
}