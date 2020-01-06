namespace Miki.Modules.Accounts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Api.Models;
    using Framework.Extension;
    using Localization.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Accounts;
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
    using Miki.Localization;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Attributes;
    using Miki.Helpers;
    using Miki.Logging;
    using Miki.Models.Objects.Backgrounds;
    using Miki.Modules.Accounts.Services;
    using Miki.Services;
    using Miki.Services.Achievements;
    using Miki.Services.Transactions;
    using Miki.Utility;

    [Module("Accounts")]
    public class AccountsModule
    {
        public AchievementService  AchievementService { get; set; }

        private readonly Net.Http.HttpClient client;

        private readonly EmojiBarSet onBarSet = new EmojiBarSet(
            "<:mbarlefton:391971424442646534>",
            "<:mbarmidon:391971424920797185>",
            "<:mbarrighton:391971424488783875>");

        private readonly EmojiBarSet offBarSet = new EmojiBarSet(
            "<:mbarleftoff:391971424824459265>",
            "<:mbarmidoff:391971424824197123>",
            "<:mbarrightoff:391971424862208000>");

        public AccountsModule(MikiApp app)
        {
            var config = app.Services.GetService<Config>();

            if (!string.IsNullOrWhiteSpace(config.MikiApiKey)
                && !string.IsNullOrWhiteSpace(config.ImageApiUrl))
            {
                client = new Net.Http.HttpClient(config.ImageApiUrl)
                    .AddHeader("Authorization", config.MikiApiKey);
            }
            else
            {
                Log.Warning("Image API can not be loaded in AccountsModule");
            }

            var discordClient = app.Services.GetService<IDiscordClient>();
            var accountsService = app.Services.GetService<AccountService>();
            var transactionService = app.Services.GetService<TransactionEvents>();

            AchievementService = app.Services.GetService<AchievementService>();


            discordClient.MessageCreate += (msg) => OnMessageCreate(app, AchievementService, msg);

            accountsService.OnLocalLevelUp += OnUserLevelUp;
            accountsService.OnLocalLevelUp += OnLevelUpAchievements;

            transactionService.OnTransactionComplete += OnTransactionComplete; 

            AchievementService.OnAchievementUnlocked += SendAchievementNotification;
            AchievementService.OnAchievementUnlocked += CheckAchievementUnlocks;
        }

        public Task OnTransactionComplete(TransactionResponse e)
        {
            Log.Message($"{e.Amount}: {e.Sender} -> {e.Receiver}.");
            return Task.CompletedTask;
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
                    }
                        break;

                    case "( ͡° ͜ʖ ͡°)":
                    {
                        var a = service.GetAchievement(AchievementIds.LennyId);
                        await service.UnlockAsync(ctx, a, arg.Author.Id);
                    }
                        break;

                    case "poi":
                    {
                        var a = service.GetAchievement(AchievementIds.ShipId);
                        await service.UnlockAsync(ctx, a, arg.Author.Id);
                    }
                        break;
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
        private async Task OnUserLevelUp(IDiscordUser user, IDiscordTextChannel channel, int level)
        {
            using var scope = MikiApp.Instance.Services.CreateScope();
            var context = scope.ServiceProvider.GetService<MikiDbContext>();
            
            var service = scope.ServiceProvider
                .GetService<ILocalizationService>();

            Locale locale = await service.GetLocaleAsync((long)channel.Id)
                .ConfigureAwait(false);

            EmbedBuilder embed = new EmbedBuilder()
                .SetTitle(locale.GetString("miki_accounts_level_up_header"))
                .SetDescription(locale.GetString(
                    "miki_accounts_level_up_content",
                    $"{user.Username}#{user.Discriminator}",
                    level))
                .SetColor(1, 0.7f, 0.2f);


            if(channel is IDiscordGuildChannel guildChannel)
            {
                IDiscordGuild guild = await guildChannel.GetGuildAsync()
                    .ConfigureAwait(false);
                long guildId = guild.Id.ToDbLong();

                List<LevelRole> rolesObtained = await context.LevelRoles
                    .Where(p => p.GuildId == guildId 
                                && p.RequiredLevel == level 
                                && p.Automatic)
                    .ToListAsync()
                    .ConfigureAwait(false);

                var notificationSetting = await Setting.GetAsync(
                        context, channel.Id, DatabaseSettingId.LevelUps)
                    .ConfigureAwait(false);

                switch((LevelNotificationsSetting)notificationSetting)
                {
                    case LevelNotificationsSetting.NONE:
                    case LevelNotificationsSetting.RewardsOnly when rolesObtained.Count == 0:
                        return;
                    case LevelNotificationsSetting.All:
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                if(rolesObtained.Count > 0)
                {
                    List<IDiscordRole> roles = (await guild.GetRolesAsync().ConfigureAwait(false))
                        .ToList();

                    IDiscordGuildUser guildUser = await guild.GetMemberAsync(user.Id)
                        .ConfigureAwait(false);
                    if(guildUser != null)
                    {
                        foreach(LevelRole role in rolesObtained)
                        {
                            IDiscordRole r = roles.FirstOrDefault(x => x.Id == (ulong)role.RoleId);
                            if(r == null)
                            {
                                continue;
                            }

                            await guildUser.AddRoleAsync(r)
                                .ConfigureAwait(false);
                        }
                    }

                    var rewards = string.Join("\n", rolesObtained
                        .Select(x => $"New Role: **{roles.FirstOrDefault(z => z.Id.ToDbLong() == x.RoleId)?.Name}**"));

                    embed.AddInlineField("Rewards", rewards);
                }
            }

            await embed.ToEmbed()
                .QueueAsync(
                    scope.ServiceProvider.GetService<MessageWorker>(),
                    scope.ServiceProvider.GetService<IDiscordClient>(),
                    channel)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Notification for user achievements
        /// </summary>
        private Task SendAchievementNotification(IContext ctx, AchievementEntry arg)
        {
            return new EmbedBuilder()
                .SetTitle($"{arg.Icon} Achievement Unlocked!")
                .SetDescription($"{ctx.GetAuthor().Username} has unlocked {arg.ResourceName}")
                .ToEmbed()
                .QueueAsync(ctx, ctx.GetChannel());
        }

        private async Task CheckAchievementUnlocks(IContext ctx, AchievementEntry arg)
        {
            var service = ctx.GetService<AchievementService>();
            var achievements = (await service.GetUnlockedAchievementsAsync((long) ctx.GetAuthor().Id))
                .ToList();
            var achievementCount = achievements.Count();
            var achievementObject = service.GetAchievement(AchievementIds.AchievementsId);

            var currentAchievements = achievements
                .Where(x => x.Name == achievementObject.Id)
                .ToList();


            if(achievementCount >= 3
               && currentAchievements.FirstOrDefault(x => x.Rank == 0) == null)
            {
                await service.UnlockAsync(ctx, achievementObject, ctx.GetAuthor().Id);
            }

            if(achievementCount >= 5
               && currentAchievements.FirstOrDefault(x => x.Rank == 1) == null)
            {
                await service.UnlockAsync(ctx, achievementObject, ctx.GetAuthor().Id, 1);
            }

            if(achievementCount >= 12
               && currentAchievements.FirstOrDefault(x => x.Rank == 2) == null)
            {
                await service.UnlockAsync(ctx, achievementObject, ctx.GetAuthor().Id, 2);
            }

            if(achievementCount >= 25
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

            IDiscordUser selectedUser = e.GetAuthor();

            if(e.GetArgumentPack().Take(out string arg))
            {
                IDiscordUser user = await e.GetGuild().FindUserAsync(arg);

                if(user != null)
                {
                    selectedUser = user;
                }
            }

            IDiscordUser discordUser = await e.GetGuild().GetMemberAsync(selectedUser.Id);
            User u = await userService.GetOrCreateUserAsync(selectedUser);

            List<Achievement> achievements = await context.Achievements
                .Where(x => x.UserId == selectedUser.Id.ToDbLong())
                .ToListAsync();

            EmbedBuilder embed = new EmbedBuilder()
                .SetAuthor($"{u.Name} | " + "Achievements", discordUser.GetAvatarUrl(), "https://miki.ai/profiles/ID/achievements");

            embed.SetColor(255, 255, 255);

            StringBuilder leftBuilder = new StringBuilder();

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
                "Total Pts: " + totalScore.ToFormattedString(),
                string.IsNullOrEmpty(leftBuilder.ToString())
                    ? locale.GetString("miki_placeholder_null")
                    : leftBuilder.ToString());

            await embed.ToEmbed().QueueAsync(e, e.GetChannel());
        }

        [Command("exp")]
        public async Task ExpAsync(IContext e)
        {
            Stream s = await client.GetStreamAsync("api/user?id=" + e.GetMessage().Author.Id);
            if(s == null)
            {
                await e.ErrorEmbed("Image generation API did not respond. This is an issue, please report it.")
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
        public async Task ProfileAsync(IContext e)
        {
            var args = e.GetArgumentPack();
            var locale = e.GetLocale();

            var context = e.GetService<MikiDbContext>();
            var userService = e.GetService<IUserService>();

            IDiscordGuildUser self = await e.GetGuild().GetSelfAsync();

            IDiscordUser discordUser;
            if(args.Take(out string arg))
            {
                discordUser = await e.GetGuild().FindUserAsync(arg);
            }
            else
            {
                discordUser = e.GetAuthor();
            }

            User account = await userService.GetOrCreateUserAsync(discordUser);

            string icon = null;
            if(await account.IsDonatorAsync(context))
            {
                icon = "https://cdn.discordapp.com/emojis/421969679561785354.png";
            }

            EmbedBuilder embed = new EmbedBuilder()
                .SetDescription(account.Title)
                .SetAuthor(
                    locale.GetString("miki_global_profile_user_header", discordUser.Username), 
                    icon, 
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

                int rank = await localExp.GetRankAsync(context);
                int localLevel = User.CalculateLevel(localExp.Experience);
                int maxLocalExp = User.CalculateLevelExperience(localLevel);
                int minLocalExp = User.CalculateLevelExperience(localLevel - 1);

                EmojiBar expBar = new EmojiBar(maxLocalExp - minLocalExp, onBarSet, offBarSet, 6);
                infoValueBuilder.AppendText(e.GetLocale().GetString(
                    "miki_module_accounts_information_level",
                    localLevel,
                    localExp.Experience.ToFormattedString(),
                    maxLocalExp.ToFormattedString()));

                if(await self.HasPermissionsAsync(GuildPermission.UseExternalEmojis))
                {
                    infoValueBuilder.AppendText(
                        expBar.Print(localExp.Experience - minLocalExp));
                }

                infoValueBuilder.AppendText(locale.GetString(
                    "miki_module_accounts_information_rank",
                    rank.ToFormattedString()));
            }
            infoValueBuilder.AppendText(
                $"Reputation: {account.Reputation:N0}",
                newLine: false);

            embed.AddInlineField(locale.GetString("miki_generic_information"), infoValueBuilder.Build());

            int globalLevel = User.CalculateLevel(account.Total_Experience);
            int maxGlobalExp = User.CalculateLevelExperience(globalLevel);
            int minGlobalExp = User.CalculateLevelExperience(globalLevel - 1);

            int? globalRank = await account.GetGlobalRankAsync(context);

            EmojiBar globalExpBar = new EmojiBar(maxGlobalExp - minGlobalExp, onBarSet, offBarSet, 6);

            var globalInfoBuilder = new MessageBuilder()
                .AppendText(locale.GetString(
                    "miki_module_accounts_information_level",
                    globalLevel.ToFormattedString(),
                    account.Total_Experience.ToFormattedString(),
                    maxGlobalExp.ToFormattedString()));
            if(await self.HasPermissionsAsync(GuildPermission.UseExternalEmojis))
            {
                globalInfoBuilder.AppendText(
                    globalExpBar.Print(maxGlobalExp - minGlobalExp));
            }

            var globalInfo = globalInfoBuilder
                .AppendText(
                    locale.GetString("miki_module_accounts_information_rank",
                        globalRank?.ToFormattedString() ?? "We haven't calculated your rank yet!"),
                        MessageFormatting.Plain,
                        false)
                .Build();

            embed.AddInlineField(
                locale.GetString("miki_generic_global_information"),
                globalInfo);

            embed.AddInlineField(
                locale.GetString("miki_generic_mekos"),
                $"{account.Currency:N0} <:mekos:421972155484471296>");

            MarriageRepository repository = new MarriageRepository(context);
            List<UserMarriedTo> marriages = (await repository.GetMarriagesAsync((long)discordUser.Id))
                .Where(x => !x.Marriage.IsProposing)
                .ToList();

            List<string> users = new List<string>();

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
                    marriageText = e.GetLocale().GetString("miki_placeholder_null");
                }

                embed.AddInlineField(
                    e.GetLocale().GetString("miki_module_accounts_profile_marriedto"),
                    marriageText);
            }

            Random r = new Random((int)(discordUser.Id - 3));
            Color c = new Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());

            embed.SetColor(c);

            List<Achievement> allAchievements = await context.Achievements
                .Where(x => x.UserId == (long)discordUser.Id)
                .ToListAsync();

            string achievements = null;
            if(allAchievements != null && allAchievements.Count > 0)
            {
                achievements = e.GetService<AchievementService>().PrintAchievements(allAchievements);
            }

            if(string.IsNullOrWhiteSpace(achievements))
            {
                achievements = e.GetLocale().GetString("miki_placeholder_null");
            }

            embed.AddInlineField(e.GetLocale().GetString("miki_generic_achievements"), achievements);
            await embed.ToEmbed().QueueAsync(e, e.GetChannel());
        }

        [Command("setbackground")]
        public async Task SetProfileBackgroundAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            if(!e.GetArgumentPack().Take(out int backgroundId))
            {
                throw new ArgumentNullException("background");
            }

            long userId = e.GetAuthor().Id.ToDbLong();
                       
            BackgroundsOwned bo = await context.BackgroundsOwned.FindAsync(userId, backgroundId);
            if(bo == null)
            {
                throw new BackgroundNotOwnedException();
            }

            ProfileVisuals v = await ProfileVisuals.GetAsync(userId, context);
            v.BackgroundId = bo.BackgroundId;
            await context.SaveChangesAsync();

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

            Background background = backgrounds.Backgrounds[id];

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
                                .WithReceiver(0L)
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
                e.GetArgumentPack().Pack.TakeAll().ToUpper(), 
                "(#)?([A-F0-9]{6})");

            if(x.Count > 0)
            {
                ProfileVisuals visuals = await ProfileVisuals.GetAsync(e.GetAuthor().Id, context);
                var hex = (x.First().Groups as IEnumerable<Group>).Last().Value;

                visuals.BackgroundColor = hex;
                await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithAmount(250)
                        .WithReceiver(0L)
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
                e.GetArgumentPack().Pack.TakeAll().ToUpper(), 
                "(#)?([A-F0-9]{6})");

            if(x.Count > 0)
            {
                ProfileVisuals visuals = await ProfileVisuals.GetAsync(e.GetAuthor().Id, context);
                var hex = (x.First().Groups as IEnumerable<Group>).Last().Value;

                visuals.ForegroundColor = hex;
                await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithAmount(250)
                        .WithReceiver(0L)
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

            List<BackgroundsOwned> backgroundsOwned = await context.BackgroundsOwned
                .Where(x => x.UserId == e.GetAuthor().Id.ToDbLong())
                .ToListAsync();

            await new EmbedBuilder()
                .SetTitle($"{e.GetAuthor().Username}'s backgrounds")
                .SetDescription(string.Join(",", backgroundsOwned.Select(x => $"`{x.BackgroundId}`")))
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("rep")]
        public async Task GiveReputationAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            User giver = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());

            var cache = e.GetService<ICacheClient>();

            var repObject = await cache.GetAsync<ReputationObject>($"user:{giver.Id}:rep");

            if(repObject == null)
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

            if(!e.GetArgumentPack().CanTake)
            {
                TimeSpan pointReset = (DateTime.Now.AddDays(1).Date - DateTime.Now);

                await new EmbedBuilder()
                {
                    Title = e.GetLocale().GetString("miki_module_accounts_rep_header"),
                    Description = e.GetLocale().GetString("miki_module_accounts_rep_description")
                }.AddInlineField(
                        e.GetLocale().GetString("miki_module_accounts_rep_total_received"),
                        giver.Reputation.ToString("N0"))
                    .AddInlineField(
                        e.GetLocale().GetString("miki_module_accounts_rep_reset"),
                        pointReset.ToTimeString(e.GetLocale()))
                    .AddInlineField(
                        e.GetLocale().GetString("miki_module_accounts_rep_remaining"),
                        repObject.ReputationPointsLeft.ToString())
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }
            else
            {
                Dictionary<IDiscordUser, short> usersMentioned = new Dictionary<IDiscordUser, short>();

                EmbedBuilder embed = new EmbedBuilder();

                int totalAmountGiven = 0;
                bool mentionedSelf = false;

                while(e.GetArgumentPack().CanTake && totalAmountGiven <= repObject.ReputationPointsLeft)
                {
                    short amount = 1;

                    e.GetArgumentPack().Take(out string userName);

                    var u = await DiscordExtensions.GetUserAsync(userName, e.GetGuild());

                    if(u == null)
                    {
                        throw new UserNullException();
                    }

                    if(e.GetArgumentPack().Take(out int value))
                    {
                        amount = (short)value;
                    }
                    else if(e.GetArgumentPack().Peek(out string arg))
                    {
                        if(Utils.IsAll(arg))
                        {
                            amount = (short)(repObject.ReputationPointsLeft - ((short)usersMentioned.Sum(x => x.Value)));
                            e.GetArgumentPack().Skip();
                        }
                    }

                    if(u.Id == e.GetAuthor().Id)
                    {
                        mentionedSelf = true;
                        continue;
                    }

                    totalAmountGiven += amount;

                    if(usersMentioned.Keys.Any(x => x.Id == u.Id))
                    {
                        usersMentioned[usersMentioned.Keys.First(x => x.Id == u.Id)] += amount;
                    }
                    else
                    {
                        usersMentioned.Add(u, amount);
                    }
                }

                if(mentionedSelf)
                {
                    embed.Footer = new EmbedFooter()
                    {
                        Text = e.GetLocale().GetString("warning_mention_self"),
                    };
                }

                if(usersMentioned.Count == 0)
                {
                    return;
                }
                else
                {
                    if(totalAmountGiven <= 0)
                    {
                        await e.ErrorEmbedResource("miki_module_accounts_rep_error_zero")
                            .ToEmbed().QueueAsync(e, e.GetChannel());
                        return;
                    }

                    if(usersMentioned.Sum(x => x.Value) > repObject.ReputationPointsLeft)
                    {
                        await e.ErrorEmbedResource(
                                "error_rep_limit", 
                                usersMentioned.Count, 
                                usersMentioned.Sum(x => x.Value), repObject.ReputationPointsLeft)
                            .ToEmbed().QueueAsync(e, e.GetChannel());
                        return;
                    }
                }

                embed.Title = (e.GetLocale().GetString("miki_module_accounts_rep_header"));
                embed.Description = (e.GetLocale().GetString("rep_success"));

                var userService = e.GetService<IUserService>();
                foreach(var u in usersMentioned)
                {
                    User receiver = await userService.GetOrCreateUserAsync(u.Key);

                    receiver.Reputation += u.Value;

                    embed.AddInlineField(
                        receiver.Name,
                        $"{(receiver.Reputation - u.Value):N0} => {receiver.Reputation:N0} (+{u.Value})"
                    );
                }

                repObject.ReputationPointsLeft -= (short)usersMentioned.Sum(x => x.Value);

                await cache.UpsertAsync(
                    $"user:{giver.Id}:rep",
                    repObject,
                    DateTime.UtcNow.AddDays(1).Date - DateTime.UtcNow
                );

                await embed.AddInlineField(e.GetLocale().GetString("miki_module_accounts_rep_points_left"), repObject.ReputationPointsLeft.ToString())
                    .ToEmbed().QueueAsync(e, e.GetChannel());

                await context.SaveChangesAsync();
            }
        }

        [Command("syncname")]
        public async Task SyncNameAsync(IContext e)
        {
            var context = e.GetService<IUserService>();
            var locale = e.GetLocale();

            User user = await context.GetOrCreateUserAsync(e.GetAuthor());
            user.Name = e.GetAuthor().Username;

            await context.SaveAsync();

            await new EmbedBuilder()
            {
                Title = "👌 OKAY",
                Description = locale.GetString("sync_success", "name")
            }.ToEmbed().QueueAsync(e, e.GetChannel());
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

            User user = await userService.GetOrCreateUserAsync(member);

            await new EmbedBuilder()
                .SetTitle("🔸 Mekos")
                .SetDescription(
                    locale.GetString("miki_user_mekos", user.Name, user.Currency.ToString("N0")))
                .SetColor(1f, 0.5f, 0.7f)
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("give")]
        public Task GiveMekosAsync(IContext e)
            => e.GetGuild().FindUserAsync(e)
                .AndThen(ThrowOnUserNull)
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

        private void ThrowOnUserNull(IDiscordGuildUser user)
        {
            if(user == null)
            {
                throw new UserNullException();
            }
        }

        [Command("daily")]
        public async Task GetDailyAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();
            var userService = e.GetService<IUserService>();
            var transactionService = e.GetService<ITransactionService>();

            User u = await userService.GetOrCreateUserAsync(e.GetAuthor());

            int dailyAmount = 100;
            int dailyStreakAmount = 20;

            if(await u.IsDonatorAsync(context))
            {
                dailyAmount *= 2;
                dailyStreakAmount *= 2;
            }

            if(u.LastDailyTime.AddHours(23) >= DateTime.UtcNow)
            {
                var time = (u.LastDailyTime.AddHours(23) - DateTime.UtcNow).ToTimeString(e.GetLocale());

                var builder = e.ErrorEmbedResource("error_daily_claimed", $"`time`");

                switch(MikiRandom.Next(2))
                {
                    case 0:
                    {
                        builder.AddInlineField(
                            "Appreciate Miki?", 
                            "Vote for us every day on [DiscordBots](https://discordbots.org/bot/160105994217586689/vote) to get an additional bonus!");
                    }
                    break;
                    case 1:
                    {
                        builder.AddInlineField(
                            "Appreciate Miki?", 
                            "Donate to us on [Patreon](https://patreon.com/mikibot) for more mekos!");
                    }
                    break;
                }
                await builder.ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            int streak = 0;
            string redisKey = $"user:{e.GetAuthor().Id}:daily";

            var cache = e.GetService<ICacheClient>();

            if(await cache.ExistsAsync(redisKey))
            {
                streak = await cache.GetAsync<int>(redisKey);
                streak++;
            }

            int amount = dailyAmount + (dailyStreakAmount * Math.Min(100, streak));

            await transactionService.CreateTransactionAsync(
                new TransactionRequest.Builder()
                    .WithAmount(amount)
                    .WithReceiver(u.Id)
                    .WithSender(0L)
                    .Build());
            u.LastDailyTime = DateTime.UtcNow;

            var embed = new EmbedBuilder()
                .SetTitle("💰 Daily")
                .SetDescription(e.GetLocale().GetString(
                    "daily_received", 
                    $"**{amount:N0}**", 
                    $"`{(u.Currency + amount):N0}`"))
                .SetColor(253, 216, 136);

            if(streak > 0)
            {
                embed.AddInlineField("Streak!", $"You're on a {streak.ToFormattedString()} day daily streak!");
            }

            await embed.ToEmbed().QueueAsync(e, e.GetChannel());

            await cache.UpsertAsync(redisKey, streak, new TimeSpan(48, 0, 0));
            await context.SaveChangesAsync();
        }
    }
}