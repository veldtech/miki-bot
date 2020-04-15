namespace Miki.Modules.Accounts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Miki.Localization;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Accounts;
    using Miki.API;
    using Miki.API.Backgrounds;
    using Miki.Api.Leaderboards;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Exceptions;
    using Miki.Bot.Models.Repositories;
    using Miki.Cache;
    using Miki.Common.Builders;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Exceptions;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Logging;
    using Miki.Modules.Accounts.Services;
    using Miki.Modules.Fun.Exceptions;
    using Miki.Net.Http;
    using Miki.Services;
    using Miki.Services.Daily;
    using Miki.Services.Achievements;
    using Miki.Services.Settings;
    using Miki.Services.Transactions;
    using Miki.Utility;

    [Module("Accounts")]
    public class AccountsModule
    {
        private static class Features
        {
            public const string ExpUsingExpV2 = "exp_using_exp_v2";
        }

        public AchievementService  AchievementService { get; set; }

        private readonly IHttpClient client;

        public AccountsModule(MikiApp app)
        {
            var config = app.Services.GetService<Config>();

            if (!string.IsNullOrWhiteSpace(config.ImageApiUrl))
            {
                client = new HttpClient(config.ImageApiUrl);
            }
            else
            {
                Log.Warning("Image API can not be loaded in AccountsModule");
            }

            var discordClient = app.Services.GetService<IDiscordClient>();
            var accountsService = app.Services.GetService<AccountService>();
            AchievementService = app.Services.GetService<AchievementService>();

            discordClient.MessageCreate += msg => OnMessageCreate(app, AchievementService, msg);

            accountsService.OnLocalLevelUp += OnUserLevelUpAsync;
            accountsService.OnLocalLevelUp += OnLevelUpAchievements;

            AchievementService.OnAchievementUnlocked += SendAchievementNotificationAsync;
            AchievementService.OnAchievementUnlocked += CheckAchievementUnlocksAsync;
        }

        private async Task OnMessageCreate(MikiApp app, AchievementService service, IDiscordMessage arg)
        {
            if(app is MikiBotApp botApp)
            {
                var ctx = await botApp.CreateFromMessageAsync(arg);
                switch(arg.Content.ToLowerInvariant())
                {
                    case "here come dat boi":
                    {
                        var a = service.GetAchievement(AchievementIds.FrogId);
                        await service.UnlockAsync(ctx, a, arg.Author.Id);
                    } break;

                    case "( ͡° ͜ʖ ͡°)":
                    {
                        var a = service.GetAchievement(AchievementIds.LennyId);
                        await service.UnlockAsync(ctx, a, arg.Author.Id);
                    } break;

                    case "poi":
                    {
                        var a = service.GetAchievement(AchievementIds.ShipId);
                        await service.UnlockAsync(ctx, a, arg.Author.Id);
                    } break;
                }

                if(MikiRandom.Next(0, 10000000000) == 5234210)
                {
                    var a = service.GetAchievement(AchievementIds.LuckId);
                    await service.UnlockAsync(ctx, a, arg.Author.Id);
                }
            }
        }

        private async Task OnLevelUpAchievements(IDiscordUser user, IDiscordTextChannel channel, int level)
        {
            var achievements = AchievementService.GetAchievement(AchievementIds.LevellingId);

            int achievementToUnlock = -1;
            if(level >= 3 && level < 5)
            {
                achievementToUnlock = 0;
            }
            else if(level >= 5 && level < 10)
            {
                achievementToUnlock = 1;
            }
            else if(level >= 10 && level < 20)
            {
                achievementToUnlock = 2;
            }
            else if(level >= 20 && level < 30)
            {
                achievementToUnlock = 3;
            }
            else if(level >= 30 && level < 50)
            {
                achievementToUnlock = 4;
            }
            else if(level >= 50 && level < 100)
            {
                achievementToUnlock = 5;
            }
            else if (level >= 100 && level < 150)
            {
                achievementToUnlock = 6;
            }
            else if (level >= 150)
            {
                achievementToUnlock = 7;
            }

            if(achievementToUnlock != -1)
            {
                if (MikiApp.Instance is MikiBotApp instance)
                {
                    await AchievementService.UnlockAsync(
                        await instance.CreateFromUserChannelAsync(user, channel),
                        achievements,
                        user.Id,
                        achievementToUnlock);
                }
            }
        }

        /// <summary>
        /// Notification for local user level ups.
        /// </summary>
        private async Task OnUserLevelUpAsync(IDiscordUser user, IDiscordTextChannel channel, int level)
        {
            using var scope = MikiApp.Instance.Services.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetService<MikiDbContext>();
            var settingsService = services.GetService<ISettingsService>();
            var localeService = services.GetService<ILocalizationService>();

            Locale locale = await localeService.GetLocaleAsync((long)channel.Id)
                .ConfigureAwait(false);

            EmbedBuilder embed = new EmbedBuilder()
                .SetTitle(locale.GetString("miki_accounts_level_up_header"))
                .SetDescription(
                    locale.GetString(
                        "miki_accounts_level_up_content", 
                        $"{user.Username}#{user.Discriminator}", 
                        level))
                .SetColor(1, 0.7f, 0.2f);


            if(channel is IDiscordGuildChannel guildChannel)
            {
                var guild = await guildChannel.GetGuildAsync().ConfigureAwait(false);
                var guildId = (long)guild.Id;

                var rolesObtained = await context.LevelRoles
                    .Where(p => p.GuildId == guildId 
                                && p.RequiredLevel == level 
                                && p.Automatic)
                    .ToListAsync()
                    .ConfigureAwait(false);

                var notificationSetting = await settingsService.GetAsync<LevelNotificationsSetting>(
                        SettingType.LevelUps, (long)channel.Id)
                    .ConfigureAwait(false);

                var setting = notificationSetting.IsValid
                    ? notificationSetting.Unwrap()
                    : LevelNotificationsSetting.RewardsOnly;
                switch(setting)
                {
                    case LevelNotificationsSetting.None:
                    case LevelNotificationsSetting.RewardsOnly when rolesObtained.Count == 0:
                        return;
                    case LevelNotificationsSetting.All:
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                if(rolesObtained.Count > 0)
                {
                    var roles = (await guild.GetRolesAsync().ConfigureAwait(false)).ToList();
                    if(user is IDiscordGuildUser guildUser)
                    {
                        foreach(var role in rolesObtained)
                        {
                            IDiscordRole r = roles.FirstOrDefault(x => x.Id == (ulong)role.RoleId);
                            if(r == null)
                            {
                                continue;
                            }

                            await guildUser.AddRoleAsync(r).ConfigureAwait(false);
                        }
                    }

                    var rewards = string.Join("\n", rolesObtained
                        .Select(x => $"New Role: **{roles.FirstOrDefault(z => z.Id.ToDbLong() == x.RoleId)?.Name}**"));

                    embed.AddInlineField("Rewards", rewards);
                }
            }

            await embed.ToEmbed()
                .QueueAsync(scope.ServiceProvider.GetService<MessageWorker>(), channel)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Notification for user achievements
        /// </summary>
        private Task SendAchievementNotificationAsync(IContext ctx, AchievementEntry arg)
        {
            return new EmbedBuilder()
                .SetTitle($"{arg.Icon} Achievement Unlocked!")
                .SetDescription($"{ctx.GetAuthor().Username} has unlocked {arg.ResourceName}")
                .ToEmbed()
                .QueueAsync(ctx, ctx.GetChannel());
        }

        private async Task CheckAchievementUnlocksAsync(IContext ctx, AchievementEntry arg)
        {
            var service = ctx.GetService<AchievementService>();
            var achievements = (await service.GetUnlockedAchievementsAsync((long) ctx.GetAuthor().Id))
                .ToList();
            var achievementObject = service.GetAchievement(AchievementIds.AchievementsId);

            var currentAchievements = achievements
                .Where(x => x.Name == achievementObject.Id)
                .ToList();


            if(achievements.Count >= 3
               && currentAchievements.FirstOrDefault(x => x.Rank == 0) == null)
            {
                await service.UnlockAsync(ctx, achievementObject, ctx.GetAuthor().Id);
            }

            if(achievements.Count >= 5
               && currentAchievements.FirstOrDefault(x => x.Rank == 1) == null)
            {
                await service.UnlockAsync(ctx, achievementObject, ctx.GetAuthor().Id, 1);
            }

            if(achievements.Count >= 12
               && currentAchievements.FirstOrDefault(x => x.Rank == 2) == null)
            {
                await service.UnlockAsync(ctx, achievementObject, ctx.GetAuthor().Id, 2);
            }

            if(achievements.Count >= 25
               && currentAchievements.FirstOrDefault(x => x.Rank == 3) == null)
            {
                await service.UnlockAsync(ctx, achievementObject, ctx.GetAuthor().Id, 3);
            }
        }

        [Command("achievements")]
        public async Task AchievementsAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();
            var userService = e.GetService<IUserService>();
            var locale = e.GetLocale();

            var selectedUser = e.GetAuthor();

            if(e.GetArgumentPack().Take(out string arg))
            {
                var user = await e.GetGuild().FindUserAsync(arg);

                if(user != null)
                {
                    selectedUser = user;
                }
            }

            var discordUser = await e.GetGuild().GetMemberAsync(selectedUser.Id);
            var u = await userService.GetOrCreateUserAsync(selectedUser);

            var achievements = await context.Achievements
                .Where(x => x.UserId == selectedUser.Id.ToDbLong())
                .ToListAsync();

            var embed = new EmbedBuilder()
                .SetAuthor($"{u.Name} | " + "Achievements", discordUser.GetAvatarUrl(), "https://miki.ai/profiles/ID/achievements");

            embed.SetColor(255, 255, 255);

            var leftBuilder = new StringBuilder();

            int totalScore = 0;
            var achievementService = e.GetService<AchievementService>();

            foreach(var a in achievements)
            {
                AchievementEntry metadata = achievementService.GetAchievement(a.Name).Entries[a.Rank];
                // TODO: Clean up or turn into resource.
                leftBuilder.AppendLine(
                    metadata.Icon + " | `" + metadata.ResourceName.PadRight(15) 
                    + $"{metadata.Points.ToString().PadLeft(3)} pts`" 
                    + $" | 📅 {a.UnlockedAt.ToShortDateString()}");
                totalScore += metadata.Points;
            }

            embed.AddInlineField(
                $"Total Pts: {totalScore:N0}",
                string.IsNullOrEmpty(leftBuilder.ToString())
                    ? locale.GetString("miki_placeholder_null")
                    : leftBuilder.ToString());

            await embed.ToEmbed().QueueAsync(e, e.GetChannel());
        }

        [Command("exp")]
        public async Task ExpAsync(IContext e)
        {
            if(!e.HasFeatureEnabled(Features.ExpUsingExpV2))
            {
                await LegacyExpAsync(e).ConfigureAwait(false);
                return;
            }

            var response = await client.GetAsync(
                $"user?id={e.GetAuthor().Id}&hash={e.GetAuthor().AvatarId}");
            if(response.Success)
            {
                await using var s = await response.HttpResponseMessage.Content.ReadAsStreamAsync();
                await e.GetChannel().SendFileAsync(s, "exp.png");
            }
            else
            {
                throw new InternalServerErrorException("image API");
            }
        }

        [Obsolete("Remove once feature flag 'exp_using_exp_v2' has rolled out")]
        private async Task LegacyExpAsync(IContext e)
        {
            var s = await client.GetStreamAsync("api/user?id=" + e.GetMessage().Author.Id);
            if(s == null)
            {
                await e.ErrorEmbed(
                        "Image generation API did not respond. This is an issue, please report it.")
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                throw new PlatformNotSupportedException("Image API");
            }

            e.GetChannel().QueueMessage(e, stream: s);
        }

        [Command("leaderboards", "lb", "leaderboard", "top")]
        public async Task LeaderboardsAsync(IContext e)
        {
            LeaderboardsOptions options = new LeaderboardsOptions();

            e.GetArgumentPack().Peek(out string argument);

            switch(argument?.ToLower() ?? "")
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

            if(e.GetArgumentPack().Peek(out string localArg))
            {
                if(localArg.ToLower() == "local")
                {
                    if(options.Type != LeaderboardsType.PASTA)
                    {
                        options.GuildId = e.GetGuild().Id;
                    }
                    e.GetArgumentPack().Skip();
                }
            }

            if(e.GetArgumentPack().Peek(out int index))
            {
                options.Offset = Math.Max(0, index - 1) * 12;
                e.GetArgumentPack().Skip();
            }

            options.Amount = 12;

            var api = e.GetService<MikiApiClient>();

            LeaderboardsObject obj = await api.GetPagedLeaderboardsAsync(options);

            await Utils.RenderLeaderboards(new EmbedBuilder(), obj.Items, obj.CurrentPage * 12)
                .SetFooter(
                    e.GetLocale().GetString(
                        "page_index", 
                        obj.CurrentPage + 1, 
                        Math.Ceiling((double)obj.TotalPages / 10)))
                .SetAuthor(
                    "Leaderboards: " + options.Type + " (click me!)",
                    null,
                    api.BuildLeaderboardsUrl(options)
                )
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("profile")]
        public class ProfileCommand
        {
            private readonly EmojiBarSet onBarSet = new EmojiBarSet(
                AppProps.Emoji.ExpBarOnStart,
                AppProps.Emoji.ExpBarOnMiddle,
                AppProps.Emoji.ExpBarOnEnd);

            private readonly EmojiBarSet offBarSet = new EmojiBarSet(
                AppProps.Emoji.ExpBarOffStart,
                AppProps.Emoji.ExpBarOffMiddle,
                AppProps.Emoji.ExpBarOffEnd);

            [Command]
            public async Task ProfileAsync(IContext e)
            {
                var args = e.GetArgumentPack();
                var locale = e.GetLocale();

                var context = e.GetService<MikiDbContext>();
                var userService = e.GetService<IUserService>();
                var leaderboardsService = e.GetService<LeaderboardsService>();

                var guild = e.GetGuild();
                IDiscordGuildUser self = null;
                if(guild != null)
                {
                    self = await guild.GetSelfAsync();
                }

                var discordUser = args.Take(out string arg)
                    ? await e.GetGuild().FindUserAsync(arg)
                    : e.GetAuthor();
                
                var account = await userService.GetOrCreateUserAsync(discordUser);

                var embed = new EmbedBuilder()
                    .SetDescription(account.Title)
                    .SetAuthor(
                        locale.GetString("miki_global_profile_user_header", discordUser.Username),
                        await GetProfileIconAsync(userService, (long)discordUser.Id),
                        "https://patreon.com/mikibot")
                    .SetThumbnail(discordUser.GetAvatarUrl());

                var infoValueBuilder = new MessageBuilder();

                if(e.GetGuild() != null)
                {
                    LocalExperience localExp = await LocalExperience.GetAsync(
                        context,
                        e.GetGuild().Id,
                        discordUser.Id);

                    if(localExp == null)
                    {
                        localExp = await LocalExperience.CreateAsync(
                            context,
                            e.GetGuild().Id,
                            discordUser.Id,
                            discordUser.Username);
                    }

                    var rank = await leaderboardsService.GetLocalRankAsync(
                        (long)e.GetGuild().Id, 
                        x => x.Experience > localExp.Experience);
                    var localLevel = User.CalculateLevel(localExp.Experience);
                    var maxLocalExp = User.CalculateLevelExperience(localLevel);
                    var minLocalExp = User.CalculateLevelExperience(localLevel - 1);

                    EmojiBar expBar = new EmojiBar(
                        maxLocalExp - minLocalExp, 
                        onBarSet, 
                        offBarSet, 
                        6);
                    infoValueBuilder.AppendText(e.GetLocale().GetString(
                        "miki_module_accounts_information_level",
                        localLevel,
                        localExp.Experience.ToString("N0"),
                        maxLocalExp.ToString("N0")));


                    if(self == null
                       || await self.HasPermissionsAsync(GuildPermission.UseExternalEmojis))
                    {
                        infoValueBuilder.AppendText(
                            expBar.Print(localExp.Experience - minLocalExp));
                    }

                    infoValueBuilder.AppendText(locale.GetString(
                        "miki_module_accounts_information_rank",
                        rank.ToString("N0")));
                }

                infoValueBuilder.AppendText(
                    $"Reputation: {account.Reputation:N0}",
                    newLine: false);

                embed.AddInlineField(locale.GetString("miki_generic_information"),
                    infoValueBuilder.Build());

                var globalLevel = User.CalculateLevel(account.Total_Experience);
                var maxGlobalExp = User.CalculateLevelExperience(globalLevel);
                var minGlobalExp = User.CalculateLevelExperience(globalLevel - 1);

                var globalRank = await leaderboardsService.GetGlobalRankAsync((long)discordUser.Id);

                var globalExpBar = new EmojiBar(
                    maxGlobalExp - minGlobalExp, 
                    onBarSet, 
                    offBarSet, 
                    6);

                var globalInfoBuilder = new MessageBuilder()
                    .AppendText(locale.GetString(
                        "miki_module_accounts_information_level",
                        globalLevel.ToString("N0"),
                        account.Total_Experience.ToString("N0"),
                        maxGlobalExp.ToString("N0")));

                if(self == null 
                   || await self.HasPermissionsAsync(GuildPermission.UseExternalEmojis))
                {
                    globalInfoBuilder.AppendText(
                        globalExpBar.Print(maxGlobalExp - minGlobalExp));
                }

                var globalInfo = globalInfoBuilder
                    .AppendText(
                        locale.GetString(
                            "miki_module_accounts_information_rank",
                            globalRank?.ToString("N0") ?? "We haven't calculated your rank yet!"),
                        MessageFormatting.Plain, false)
                    .Build();

                embed.AddInlineField(
                    locale.GetString("miki_generic_global_information"),
                    globalInfo);

                embed.AddInlineField(
                    locale.GetString("miki_generic_mekos"),
                    $"{account.Currency:N0} {AppProps.Emoji.Mekos}");

                var repository = e.GetService<MarriageService>();
                var marriages =
                    (await repository.GetMarriagesAsync((long)discordUser.Id))
                    .Where(x => !x.Marriage.IsProposing)
                    .ToList();

                var users = new List<string>();

                int maxCount = marriages.Count;

                for(int i = 0; i < maxCount; i++)
                {
                    users.Add((await e.GetService<IDiscordClient>()
                        .GetUserAsync(marriages[i].GetOther(discordUser.Id))).Username);
                }

                if(marriages.Count > 0)
                {
                    List<string> marriageStrings = new List<string>();

                    for(int i = 0; i < maxCount; i++)
                    {
                        if(marriages[i].GetOther((long)discordUser.Id) == 0)
                        {
                            continue;
                        }

                        marriageStrings.Add(
                            $"💕 {users[i]} (_{marriages[i].Marriage.TimeOfMarriage.ToShortDateString()}_)");
                    }

                    string marriageText = string.Join("\n", marriageStrings);
                    if(string.IsNullOrEmpty(marriageText))
                    {
                        marriageText = locale.GetString("miki_placeholder_null");
                    }

                    embed.AddInlineField(
                        locale.GetString("miki_module_accounts_profile_marriedto"),
                        marriageText);
                }

                var random = new Random((int)(discordUser.Id - 3));
                embed.SetColor(
                    (float)random.NextDouble(), 
                    (float)random.NextDouble(), 
                    (float)random.NextDouble());

                List<Achievement> allAchievements = await context.Achievements
                    .Where(x => x.UserId == (long)discordUser.Id)
                    .ToListAsync();

                var achievementService = e.GetService<AchievementService>();
                string achievements = allAchievements?.Any() ?? false
                    ? achievementService.PrintAchievements(allAchievements)
                    : locale.GetString("miki_placeholder_null");

                embed.AddInlineField(
                    locale.GetString("miki_generic_achievements"), achievements);

                await embed.ToEmbed()
                    .QueueAsync(e, e.GetChannel());
            }

            private async Task<string> GetProfileIconAsync(IUserService service, long id)
            {
                if(await service.UserIsDonatorAsync(id))
                {
                    return "https://cdn.discordapp.com/emojis/421969679561785354.png";
                }
                return null;
            }
        }

        [Command("setbackground")]
        public async Task SetProfileBackgroundAsync(IContext e)
        {
            var unit = e.GetService<IUnitOfWork>();
            var backgroundRepository = unit.GetRepository<BackgroundsOwned>();
            var profileVisualRepository = unit.GetRepository<ProfileVisuals>();

            var backgroundId = e.GetArgumentPack().TakeRequired<int>();
            long userId = (long)e.GetAuthor().Id;
                       
            var bo = await backgroundRepository.GetAsync(userId, backgroundId);
            if(bo == null)
            {
                throw new BackgroundNotOwnedException();
            }

            var v = await profileVisualRepository.GetAsync(userId);
            v.BackgroundId = bo.BackgroundId;

            await profileVisualRepository.EditAsync(v);
            await unit.CommitAsync();

            // TODO: redo embed.
            await e.SuccessEmbed("Successfully set background.")
                .QueueAsync(e, e.GetChannel());
        }

        [Command("buybackground")]
        public async Task BuyProfileBackgroundAsync(IContext e)
        {
            var backgrounds = e.GetService<BackgroundStore>();
            var transactionService = e.GetService<ITransactionService>();

            if(!e.GetArgumentPack().Take(out int id))
            {
                e.GetChannel().QueueMessage(e, null, "Enter a number after `>buybackground` to check the backgrounds! (e.g. >buybackground 1)");
            }

            if(id >= backgrounds.Backgrounds.Count || id < 0)
            {
                await e.ErrorEmbed("This background does not exist!")
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            var background = backgrounds.Backgrounds[id];

            var embed = new EmbedBuilder()
                .SetTitle("Buy Background")
                .SetImage(background.ImageUrl);

            if(background.Price > 0)
            {
                embed.SetDescription(
                    $"This background for your profile will cost {background.Price:N0} mekos, Type `>buybackground {id} yes` to buy.");
            }
            else
            {
                embed.SetDescription("This background is not for sale.");
            }

            if(e.GetArgumentPack().Take(out string confirmation))
            {
                if(confirmation.ToLower() != "yes")
                {
                    return;
                }

                if(background.Price > 0)
                {
                    var context = e.GetService<MikiDbContext>();

                    long userId = (long)e.GetAuthor().Id;

                    var hasBackground = await context.BackgroundsOwned
                        .AnyAsync(x => x.BackgroundId == background.Id && x.UserId == userId);

                    if(!hasBackground)
                    {
                        await transactionService.CreateTransactionAsync(
                            new TransactionRequest.Builder()
                                .WithAmount(background.Price)
                                .WithReceiver(AppProps.Currency.BankId)
                                .WithSender(userId)
                                .Build());

                        await context.BackgroundsOwned.AddAsync(new BackgroundsOwned()
                        {
                            UserId = e.GetAuthor().Id.ToDbLong(),
                            BackgroundId = background.Id,
                        });

                        await context.SaveChangesAsync();
                        await e.SuccessEmbed("Background purchased!")
                            .QueueAsync(e, e.GetChannel());
                    }
                    else
                    {
                        throw new BackgroundOwnedException();
                    }
                }
            }

            await embed.ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("setbackcolor")]
        public async Task SetProfileBackColorAsync(IContext e)
        {
            var transactionService = e.GetService<ITransactionService>();
            var context = e.GetService<MikiDbContext>();

            var x = Regex.Matches(
                e.GetArgumentPack().Pack.TakeAll().ToUpperInvariant(), 
                "(#)?([A-F0-9]{6})");

            if(x.Any())
            {
                ProfileVisuals visuals = await ProfileVisuals.GetAsync(e.GetAuthor().Id, context);
                var hex = (x.First().Groups as IEnumerable<Group>).Last().Value;

                visuals.BackgroundColor = hex;
                await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithAmount(250)
                        .WithReceiver(AppProps.Currency.BankId)
                        .WithSender((long)e.GetAuthor().Id)
                        .Build());

                await context.SaveChangesAsync();

                await e.SuccessEmbed("Your foreground color has been successfully " +
                                     $"changed to `{hex}`")
                    .QueueAsync(e, e.GetChannel());
            }
            else
            {
                await new EmbedBuilder()
                    .SetTitle("🖌 Setting a background color!")
                    .SetDescription("Changing your background color costs 250 mekos. " +
                                    "use `>setbackcolor (e.g. #00FF00)` to purchase")
                    .ToEmbed().QueueAsync(e, e.GetChannel());
            }
        }

        [Command("setfrontcolor")]
        public async Task SetProfileForeColorAsync(IContext e)
        {
            var transactionService = e.GetService<ITransactionService>();
            var context = e.GetService<MikiDbContext>();

            var x = Regex.Matches(
                e.GetArgumentPack().Pack.TakeAll().ToUpperInvariant(), 
                "(#)?([A-F0-9]{6})");

            if(x.Any())
            {
                ProfileVisuals visuals = await ProfileVisuals.GetAsync(e.GetAuthor().Id, context);
                var hex = (x.First().Groups as IEnumerable<Group>).Last().Value;

                visuals.ForegroundColor = hex;
                await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithAmount(250)
                        .WithReceiver(AppProps.Currency.BankId)
                        .WithSender((long)e.GetAuthor().Id)
                        .Build());
                await context.SaveChangesAsync();

                await e.SuccessEmbed($"Your foreground color has been successfully changed to `{hex}`")
                    .QueueAsync(e, e.GetChannel());
            }
            else
            {
                await new EmbedBuilder()
                    .SetTitle("🖌 Setting a foreground color!")
                    .SetDescription("Changing your foreground(text) color costs 250 " +
                                    "mekos. use `>setfrontcolor (e.g. #00FF00)` to purchase")
                    .ToEmbed().QueueAsync(e, e.GetChannel());
            }
        }

        [Command("backgroundsowned")]
        public async Task BackgroundsOwnedAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            var backgroundsOwned = await context.BackgroundsOwned
                .Where(x => x.UserId == e.GetAuthor().Id.ToDbLong())
                .ToListAsync();

            await new EmbedBuilder()
                .SetTitle($"{e.GetAuthor().Username}'s backgrounds")
                .SetDescription(string.Join(",", backgroundsOwned.Select(x => $"`{x.BackgroundId}`")))
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("rep")]
        public class GiveReputationCommand
        {
            [Command]
            public async Task GiveReputationAsync(IContext e)
            {
                var locale = e.GetLocale();
                var userService = e.GetService<IUserService>();

                var giver = await userService.GetOrCreateUserAsync(e.GetAuthor());
                var repObject = await userService.GetUserReputationPointsAsync(giver.Id);

                if(!e.GetArgumentPack().CanTake)
                {
                    await ReputationInfoScreenAsync(e, giver, repObject)
                        .ConfigureAwait(false);
                    return;
                }

                var usersMentioned = new Dictionary<ulong, GiveReputationRequest>();
                var embed = new EmbedBuilder();

                var mentionedSelf = false;
                while(e.GetArgumentPack().CanTake)
                {
                    e.GetArgumentPack().Take(out string userName);
                    var currentUser = await e.GetGuild().FindUserAsync(userName)
                        .ConfigureAwait(false);

                    var amount = 1;
                    if(e.GetArgumentPack().Take(out int value))
                    {
                        amount = value;
                    }
                    else if(e.GetArgumentPack().Peek(out string arg))
                    {
                        if(Utils.IsAll(arg))
                        {
                            amount = repObject.ReputationPointsLeft
                                     - usersMentioned.Sum(x => x.Value.Amount);
                            e.GetArgumentPack().Skip();
                        }
                    }

                    if(currentUser.Id == e.GetAuthor().Id)
                    {
                        mentionedSelf = true;
                        continue;
                    }

                    if(usersMentioned.Keys.Any(x => x == currentUser.Id))
                    {
                        usersMentioned[currentUser.Id].Amount += amount;
                    }
                    else
                    {
                        usersMentioned.Add(currentUser.Id, new GiveReputationRequest
                        {
                            RecieverId = (long)currentUser.Id,
                            Amount = amount
                        });
                    }
                }

                if(mentionedSelf)
                {
                    embed.Footer = new EmbedFooter
                    {
                        Text = locale.GetString("warning_mention_self"),
                    };
                }

                var result = await userService.GiveReputationAsync(
                    (long)e.GetAuthor().Id, 
                    usersMentioned.Select(x => x.Value).ToList())
                    .ConfigureAwait(false);

                foreach(var user in result.UsersReceived)
                {
                    var points = usersMentioned[(ulong)user.Id].Amount;
                    embed.AddInlineField(
                        user.Name,
                        $"{user.Reputation - points:N0} => {user.Reputation:N0} (+{points})");
                }
                
                await embed.SetTitle(locale.GetString("miki_module_accounts_rep_header"))
                    .SetDescription(locale.GetString("rep_success"))
                    .AddInlineField(
                        locale.GetString("miki_module_accounts_rep_points_left"),
                        result.ReputationLeft.ToString())
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
            }

            private async Task ReputationInfoScreenAsync(
                IContext e, User giver, ReputationObject repObject)
            {
                var locale = e.GetLocale();
                var pointReset = DateTime.Now.AddDays(1).Date - DateTime.Now;

                await new EmbedBuilder()
                    .SetTitle(locale.GetString("miki_module_accounts_rep_header"))
                    .SetDescription(locale.GetString("miki_module_accounts_rep_description"))
                    .AddInlineField(
                        locale.GetString("miki_module_accounts_rep_total_received"),
                        giver.Reputation.ToString("N0"))
                    .AddInlineField(
                        locale.GetString("miki_module_accounts_rep_reset"),
                        pointReset.ToTimeString(locale))
                    .AddInlineField(
                        locale.GetString("miki_module_accounts_rep_remaining"),
                        repObject.ReputationPointsLeft.ToString())
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
            }
        }

        [Command("syncname")]
        public async Task SyncNameAsync(IContext e)
        {
            var context = e.GetService<IUserService>();

            User user = await context.GetOrCreateUserAsync(e.GetAuthor()).ConfigureAwait(false);
            user.Name = e.GetAuthor().Username;

            await context.UpdateUserAsync(user).ConfigureAwait(false);
            await context.SaveAsync().ConfigureAwait(false);

            await e.SuccessEmbedResource("sync_success", "name")
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }

        [Command("mekos", "bal", "meko")]
        public async Task ShowMekosAsync(IContext e)
        {
            var userService = e.GetService<IUserService>();
            var locale = e.GetLocale();

            IDiscordUser member = e.GetAuthor();
            if(e.GetArgumentPack().Take(out string value))
            {
                member = await e.GetGuild().FindUserAsync(value);
            }

            var user = await userService.GetOrCreateUserAsync(member);
            var desc = locale.GetString("miki_user_mekos", user.Name, user.Currency.ToString("N0"));

            await new EmbedBuilder()
                .SetTitle("🔸 Mekos")
                .SetDescription(desc)
                .SetColor(1f, 0.5f, 0.7f)
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("give")]
        public Task GiveMekosAsync(IContext e)
            => e.GetGuild().FindUserAsync(e)
                .Map(receiver => new TransactionRequest.Builder()
                    .WithReceiver((long)receiver.Id)
                    .WithSender((long)e.GetAuthor().Id)
                    .WithAmount(e.GetArgumentPack().TakeRequired<int>())
                    .Build())
                .Map(request => e.GetService<ITransactionService>()
                    .CreateTransactionAsync(request))
                .AndThen(transaction => CreateTransactionEmbed(e, transaction)
                    .QueueAsync(e, e.GetChannel()));

        public DiscordEmbed CreateTransactionEmbed(IContext context, TransactionResponse transaction)
        {
            return new EmbedBuilder()
                .SetTitle("🔸 transaction")
                .SetDescription(context.GetLocale().GetString(
                    "give_description",
                    transaction.Sender.Name,
                    transaction.Receiver.Name,
                    transaction.Amount.ToString("N0")))
                .SetColor(255, 140, 0)
                .ToEmbed();
        }

        [Command("daily")]
        public class DailyCommand
        {
            [Command]
            public async Task ClaimDailyAsync(IContext e)
            {
                var userService = e.GetService<IUserService>();
                var dailyService = e.GetService<IDailyService>();
                var locale = e.GetLocale();

                var user = await userService.GetOrCreateUserAsync(e.GetAuthor())
                    .ConfigureAwait(false);
                var response = await dailyService.ClaimDailyAsync(user.Id)
                    .ConfigureAwait(false);

                var embed = new EmbedBuilder()
                    .SetTitle(locale.GetString("daily_title"))
                    .SetColor(253, 216, 136);

                if (response.Status == DailyStatus.NotReady)
                {
                    var time = (response.LastClaimTime.AddHours(23) - DateTime.UtcNow).ToTimeString(locale);
                    embed.SetDescription(locale.GetString(
                        "daily_claimed",
                        $"`{time}`"));

                    var appreciationString = MikiRandom.Of(new[]
                    {
                        locale.GetString("appreciate_patreon", "[DiscordBots](https://discordbots.org/bot/160105994217586689/vote)"),
                        locale.GetString("appreciate_vote", "[Patreon](https://patreon.com/mikibot)")
                    });

                    embed.AddInlineField(locale.GetString("appreciate_title"), appreciationString)
                        .AddInlineField(locale.GetString("daily_stats_current_streak"), $"{response.CurrentStreak}");

                    await embed.ToEmbed()
                        .QueueAsync(e, e.GetChannel());
                    return;
                }

                embed.SetDescription(locale.GetString(
                    "daily_received",
                    $"**{response.AmountClaimed:N0}**",
                    $"`{(user.Currency + response.AmountClaimed):N0}`"));

                if (response.CurrentStreak > 0)
                {
                    embed.AddInlineField(
                        locale.GetString("daily_streak_title"),
                        locale.GetString("daily_streak", $"{response.CurrentStreak:N0}"));
                }

                await embed.ToEmbed().QueueAsync(e, e.GetChannel());
            }

            [Command("stats")]
            public async Task DailyStatsAsync(IContext e)
            {
                var dailyService = e.GetService<IDailyService>();
                var daily = await dailyService.GetOrCreateDailyAsync((long)e.GetAuthor().Id);

                var locale = e.GetLocale();
                var timeLeft = daily.LastClaimTime.AddHours(23) - DateTime.UtcNow;
                var timeString = timeLeft.Seconds > 0 ? timeLeft.ToTimeString(locale) : "Now!";

                var embed = new EmbedBuilder()
                    .SetTitle(locale.GetString("daily_stats_title", $"{e.GetAuthor().Username}"))
                    .SetColor(253, 216, 136)
                    .AddInlineField(locale.GetString("daily_stats_current_streak"), $"{daily.CurrentStreak}")
                    .AddInlineField(locale.GetString("daily_stats_longest_streak"), 
                        $"{(daily.LongestStreak >= daily.CurrentStreak ? daily.LongestStreak:daily.CurrentStreak)}")
                    .AddInlineField(locale.GetString("daily_stats_time"), $"{timeString}");

                await embed.ToEmbed().QueueAsync(e, e.GetChannel());
            }
        }
    }
}
