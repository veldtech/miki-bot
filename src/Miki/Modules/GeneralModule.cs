namespace Miki.Modules
{
    using Miki.Framework.Commands.Permissions.Attributes;
    using Miki.Framework.Commands.Permissions.Models;
    using Miki.Framework.Commands.Prefixes;
    using Microsoft.EntityFrameworkCore;
    using Miki.API;
    using Miki.Bot.Models;
    using Miki.Cache;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Rest;
    using Miki.Dsl;
    using Miki.Framework;
    using Miki.Framework.Arguments;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Nodes;
    using Miki.Framework.Language;
    using Miki.Helpers;
    using Miki.Localization;
    using Miki.Localization.Models;
    using NCalc;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Miki.API.Reminders;
    using Miki.Attributes;
    using Miki.Modules.Accounts.Services;
    using Miki.Services.Achievements;
    using Miki.Utility;
    using Miki.Bot.Models.Exceptions;
    using Miki.Framework.Commands.Permissions;
    using Miki.Framework.Commands.Scopes;
    using Miki.Framework.Commands.Scopes.Attributes;
    using Miki.Localization.Exceptions;

    [Module("General")]
	public class GeneralModule
	{
        private readonly TaskScheduler<string> taskScheduler = new TaskScheduler<string>();

        [Command("avatar")]
        public async Task AvatarAsync(IContext e)
        {
            
            string avatarResource = e.GetAuthor().Username;
            string avatarUrl = e.GetAuthor().GetAvatarUrl();

            if(e.GetArgumentPack().Take(out string arg))
            {
                if(arg == "-s")
                {
                    avatarResource = e.GetGuild().Name;
                    avatarUrl = e.GetGuild().IconUrl;
                }
                else
                {
                    IDiscordGuildUser user = await e.GetGuild()
                        .FindUserAsync(arg)
                        .ConfigureAwait(false);
                    if(user == null)
                    {
                        throw new UserNullException();
                    }

                    avatarResource = user.Username;
                    avatarUrl = user.GetAvatarUrl();
                }
            }

            await new EmbedBuilder()
                .SetTitle($"🖼 Avatar for {avatarResource}")
                .SetThumbnail(avatarUrl)
                .SetColor(215, 158, 132)
                .AddInlineField("Full image", $"[click here]({avatarUrl})")
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("calc", "calculate")]
        public Task CalculateAsync(IContext e)
        {
            var expressionString = e.GetArgumentPack().Pack.TakeAll();
            expressionString = expressionString.Trim('\'');

            Expression expression = new Expression(expressionString, EvaluateOptions.NoCache);
            expression.Parameters.Add("pi", Math.PI);

            expression.EvaluateFunction += (name, x) =>
            {
                switch(name)
                {
                    case "lerp":
                    {
                        double n = (double) x.Parameters[0].Evaluate();
                        double v = (double) x.Parameters[1].Evaluate();
                        double o = (double) x.Parameters[2].Evaluate();
                        x.Result = (n * (1.0 - o)) + (v * o);
                        break;
                    }
                }
            };

            Result<string> result = new Result<string>(() => expression.Evaluate().ToString());
            if(result.IsValid)
            {
                return new EmbedBuilder()
                    .SetTitle("🧮  Calculator")
                    .SetDescription(Utils.EscapeEveryone(result.Unwrap()))
                    .SetColor(213, 171, 136)
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
            }

            var exception = result.UnwrapException();
            if(exception is LocalizedException le)
            {
                return e.ErrorEmbedResource(le.LocaleResource)
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
            }
            return e.ErrorEmbed($"Your calculation threw an error: {exception.Message}")
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("changelog")]
        public Task ChangelogAsync(IContext e)
        {
            return new EmbedBuilder()
                .SetTitle("Changelog")
                .SetDescription("Check out my changelog blog [here](https://blog.miki.ai/)!")
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("giveaway")]
        public async Task GiveawayAsync(IContext e)
        {
            DiscordEmoji.TryParse("🎁", out var emoji);

            var args = e.GetArgumentPack();

            args.Take(out string giveawayText);
            if(args.CanTake)
            {
                while(!e.GetArgumentPack().Pack.Peek().StartsWith("-"))
                {
                    giveawayText += " " + e.GetArgumentPack().Pack.Take();
                }
            }

            var mml = new MMLParser(e.GetArgumentPack().Pack.TakeAll()).Parse();

            int amount = mml.Get("amount", 1);
            TimeSpan timeLeft = mml.Get("time", "1h").GetTimeFromString();

            if(amount > 10)
            {
                await e.ErrorEmbed("We can only allow up to 10 picks per giveaway")
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
                return;
            }

            giveawayText += ((amount > 1) ? " x " + amount : "");

            List<IDiscordUser> winners = new List<IDiscordUser>();

            IDiscordMessage msg = await CreateGiveawayEmbed(e, giveawayText)
                .AddField("Time", timeLeft.ToTimeString(e.GetLocale()), true)
                .AddField("React to participate", "good luck", true)
                .ToEmbed()
                .SendToChannel(e.GetChannel())
                .ConfigureAwait(false);

            await msg.CreateReactionAsync(emoji)
                .ConfigureAwait(false);

            int updateTask = -1;

            taskScheduler.AddTask(e.GetAuthor().Id, async (desc) =>
            {
                msg = await e.GetChannel()
                    .GetMessageAsync(msg.Id)
                    .ConfigureAwait(false);
                if(msg == null)
                {
                    return;
                }

                await msg.DeleteReactionAsync(emoji)
                    .ConfigureAwait(false);

                await Task.Delay(1000)
                    .ConfigureAwait(false);

                var reactions = (await msg.GetReactionsAsync(emoji)
                        .ConfigureAwait(false))
                    .ToList();

                //do
                //{
                //	reactions.AddRange();
                //	reactionsGained += 100;
                //} while (reactions.Count == reactionsGained);

                // Select random winners
                for(int i = 0; i < amount; i++)
                {
                    if(!reactions.Any())
                    {
                        break;
                    }

                    int index = MikiRandom.Next(reactions.Count());
                    winners.Add(reactions.ElementAtOrDefault(index));
                }

                if(updateTask != -1)
                {
                    taskScheduler.CancelReminder(e.GetAuthor().Id, updateTask);
                }

                string winnerText = string.Join(
                    "\n", winners.Select(x => $"{x.Username}#{x.Discriminator}").ToArray());
                if(string.IsNullOrEmpty(winnerText))
                {
                    winnerText = "nobody!";
                }

                await msg.EditAsync(new EditMessageArgs
                {
                    Embed = CreateGiveawayEmbed(e, giveawayText)
                        .AddField("Winners", winnerText)
                        .ToEmbed()
                }).ConfigureAwait(false);
            }, "description var", timeLeft);
        }

        private EmbedBuilder CreateGiveawayEmbed(IContext e, string text)
		{
			return new EmbedBuilder()
			{
				Author = new EmbedAuthor()
				{
					Name = e.GetAuthor().Username + " is giving away",
					IconUrl = e.GetAuthor().GetAvatarUrl()
				},
				ThumbnailUrl = "https://i.imgur.com/rIDHtwN.png",
				Description = text,
				Color = new Color(253, 216, 136),
			};
		}

		[Command("guildinfo")]
        [GuildOnly]
		public async Task GuildInfoAsync(IContext e)
		{
			var guild = e.GetGuild();
			var locale = e.GetLocale();

			IDiscordGuildUser owner = await guild.GetOwnerAsync()
                .ConfigureAwait(false);

            var context = e.GetService<MikiDbContext>();
			var cache = e.GetService<ICacheClient>();

			string prefix = await e.GetService<PrefixService<IDiscordMessage>>()
				.GetDefaultTrigger()
				.GetForGuildAsync(context, cache, guild.Id)
                .ConfigureAwait(false);

            var roles = (await e.GetGuild().GetRolesAsync().ConfigureAwait(false))
                .ToList();
            var channels = (await e.GetGuild().GetChannelsAsync().ConfigureAwait(false))
                .ToList();

            var builder = new EmbedBuilder()
			{
				Author = new EmbedAuthor()
				{
					Name = guild.Name,
					IconUrl = guild.IconUrl,
					Url = guild.IconUrl
				},
			};
			builder.AddInlineField(
				"👑 " + locale.GetString("miki_module_general_guildinfo_owned_by"),
				$"{owner.Username}#{owner.Discriminator}");

			builder.AddInlineField(
				"👉 " + locale.GetString("miki_label_prefix"),
				prefix);

			builder.AddInlineField(
				"📺 " + locale.GetString("miki_module_general_guildinfo_channels"),
				channels.Count(x => x.Type == ChannelType.GUILDTEXT).ToString("N0"));

			builder.AddInlineField(
				"🔊 " + locale.GetString("miki_module_general_guildinfo_voicechannels"),
				channels.Count(x => x.Type == ChannelType.GUILDVOICE).ToString("N0"));

			builder.AddInlineField(
				"🙎 " + locale.GetString("miki_module_general_guildinfo_users"),
				roles.Count().ToString("N0"));

			builder.AddInlineField(
				"#⃣ " + locale.GetString("miki_module_general_guildinfo_roles_count"),
				roles.Count().ToString("N0"));

			builder.AddField(
				"📜 " + locale.GetString("miki_module_general_guildinfo_roles"),
				string.Join(",", roles.Select(x => $"`{x.Name}`")));

            await builder.ToEmbed()
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }

		[Command("help")]
		public async Task HelpAsync(IContext e)
		{
			var commandHandler = e.GetService<CommandTree>();
            var locale = e.GetLocale();

            if (e.GetArgumentPack().Take(out string arg))
            {
                var command = commandHandler.GetCommand(new ArgumentPack(arg.Split(' ')));
                string prefix = e.GetPrefixMatch();

                if (command == null)
                {
                    var helpListEmbed = new EmbedBuilder
                    {
                        Title = locale.GetString("miki_module_help_error_null_header"),
                        Description = locale.GetString("miki_module_help_error_null_message", prefix),
                        Color = new Color(0.6f, 0.6f, 1.0f)
                    };

                    var firstModule = commandHandler.Root.Children.OfType<NodeModule>().First();
                    var root = (firstModule.Parent as NodeRoot);

                    List<Node> nodes = new List<Node>();
                    await foreach (var x in root.GetAllExecutableAsync(e))
                    {
                        nodes.Add(x);
                    }

                    var comparer = new API.StringComparison.StringComparer(
                        nodes.SelectMany(node => node.Metadata.Identifiers));
                    var best = comparer.GetBest(arg);

                    helpListEmbed.AddField(locale.GetString("miki_common_suggestion"), best.text);

                    await helpListEmbed.ToEmbed()
                        .QueueAsync(e, e.GetChannel())
                        .ConfigureAwait(false);
                }
                else
                {
                    if (!await command.ValidateRequirementsAsync(e)
                        .ConfigureAwait(false))
                    {
                        return;
                    }

                    var commandId = command.Metadata.Identifiers.First();

                    EmbedBuilder explainedHelpEmbed = new EmbedBuilder()
                        .SetTitle(commandId.ToUpper());

                    if (command.Metadata.Identifiers.Count > 1)
                    {
                        explainedHelpEmbed.AddInlineField(
                            e.GetLocale().GetString("miki_module_general_help_aliases"),
                            string.Join(", ", command.Metadata.Identifiers.Skip(1)));
                    }

                    explainedHelpEmbed.AddField
                    (
                        locale.GetString("miki_module_general_help_description"),
                        locale.GetString("miki_command_description_" + commandId.ToLower())
                        ?? locale.GetString("miki_placeholder_null"));

                    explainedHelpEmbed.AddField(
                        locale.GetString("miki_module_general_help_usage"),
                        locale.GetString("miki_command_usage_" + commandId.ToLower())
                        ?? locale.GetString("miki_placeholder_null"));

                    await explainedHelpEmbed.ToEmbed()
                        .QueueAsync(e, e.GetChannel())
                        .ConfigureAwait(false);
                }

                return;
            }

            await new EmbedBuilder()
			{
				Description = e.GetLocale().GetString("miki_module_general_help_dm"),
				Color = new Color(0.6f, 0.6f, 1.0f)
			}.ToEmbed()
				.QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);

            var embedBuilder = new EmbedBuilder();

            var permissionService = e.GetService<PermissionService>();
            var scopesService = e.GetService<ScopeService>();

			foreach(var nodeModule in commandHandler.Root.Children.OfType<NodeModule>())
			{
				var id = nodeModule.Metadata.Identifiers.First();

                List<Node> nodes = new List<Node>();
                await foreach (var node in nodeModule.GetAllExecutableAsync(e))
                {
                    if((await permissionService.GetPriorityPermissionAsync(e))
                       .Status != PermissionStatus.Allow)
                    {
                        continue;
                    }

                    var requiresScopeAttributes = node.Attributes.OfType<RequiresScopeAttribute>()
                        .ToList();
                    bool hasAllScopes = true;
                    foreach(var scope in requiresScopeAttributes)
                    {
                        if(await scopesService.HasScopeAsync((long)e.GetAuthor().Id, scope.ScopeId))
                        {
                            continue;
                        }
                        hasAllScopes = false;
                        break;
                    }

                    if(!hasAllScopes)
                    {
                        continue;
                    }

                    if(await node.ValidateRequirementsAsync(e))
                    {
                        nodes.Add(node);
                    }
                }
                var commandNames = string.Join(", ", nodes
                    .Select(node => '`' + node.Metadata.Identifiers.First() + '`'));

				if(!string.IsNullOrEmpty(commandNames))
				{
					embedBuilder.AddField(id.ToUpper(), commandNames);
				}
			}

			await embedBuilder.ToEmbed()
				.QueueAsync(
                    e,
                    await e.GetAuthor().GetDMChannelAsync().ConfigureAwait(false), 
                    "Join our support server: https://discord.gg/39Xpj7K")
                .ConfigureAwait(false);
        }

		[Command("donate")]
		public async Task DonateAsync(IContext e)
		{
			await new EmbedBuilder()
			{
				Title = "Hi everyone!",
				Description = e.GetLocale().GetString("miki_module_general_info_donate_string"),
				Color = new Color(0.8f, 0.4f, 0.4f),
			}.AddField("Links", "https://www.patreon.com/mikibot - if you want to donate every month and get cool rewards!\nhttps://ko-fi.com/velddy - one time donations please include your discord name#identifiers so i can contact you!", true)
			.AddField("Don't have money?", "You can always support us in different ways too! Please participate in our [idea](https://suggestions.miki.ai/) discussions so we can get a better grasp of what you guys would like to see next! Or vote for Miki on [Discordbots.org](https://discordbots.org/bot/160105994217586689)", true)
			.ToEmbed()
            .QueueAsync(e, e.GetChannel())
            .ConfigureAwait(false);
        }

		[Command("info", "about")]
		public async Task InfoAsync(IContext e)
		{
			Version v = Assembly.GetEntryAssembly()?.GetName().Version;

			EmbedBuilder embed = new EmbedBuilder()
				.SetAuthor($"Miki {v}")
				.SetColor(0.6f, 0.6f, 1.0f);

			embed.AddField(e.GetLocale().GetString("miki_module_general_info_made_by_header"),
				e.GetLocale().GetString("miki_module_general_info_made_by_description") + " Fuzen, IA, Rappy, Tal, Vallode, GrammarJew");

			embed.AddField(e.GetLocale().GetString("miki_module_general_info_links"),
				$"`{e.GetLocale().GetString("miki_module_general_info_docs").PadRight(15)}:` [documentation](https://www.github.com/velddev/miki/wiki)\n" +
				$"`{"donate".PadRight(15)}:` [patreon](https://www.patreon.com/mikibot) | [ko-fi](https://ko-fi.com/velddy)\n" +
				$"`{e.GetLocale().GetString("miki_module_general_info_twitter").PadRight(15)}:` [veld](https://www.twitter.com/velddev) | [miki](https://www.twitter.com/miki_discord)\n" +
				$"`{e.GetLocale().GetString("miki_module_general_info_reddit").PadRight(15)}:` [/r/mikibot](https://www.reddit.com/r/mikibot) \n" +
				$"`{e.GetLocale().GetString("miki_module_general_info_server").PadRight(15)}:` [discord](https://discord.gg/39Xpj7K)\n" +
				$"`{e.GetLocale().GetString("miki_module_general_info_website").PadRight(15)}:` [link](https://miki.ai) | [suggestions](https://suggestions.miki.ai/)");

			await embed.ToEmbed()
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);

            var service = e.GetService<AchievementService>();
            await service.UnlockAsync(
                e, service.GetAchievement(AchievementIds.ReadInfoId), e.GetAuthor().Id);
        }

		[Command("invite")]
		public async Task InviteAsync(IContext e)
		{
			e.GetChannel().QueueMessage(e, null, e.GetLocale().GetString("miki_module_general_invite_message"));
            var dmChannel = await e.GetAuthor().GetDMChannelAsync()
                    .ConfigureAwait(false);
            dmChannel.QueueMessage(e,
                null,
                e.GetLocale().GetString("miki_module_general_invite_dm")
                + "\n" + AppProps.InviteUrl);
        }

        [Command("ping", "lag")]
        public Task PingAsync(IContext e)
        {
            return new LocalizedEmbedBuilder(e.GetLocale())
                .WithTitle(new RawResource("Ping"))
                .WithDescription("ping_placeholder")
                .Build()
                .QueueAsync(e,
                    e.GetChannel(),
                    modifier: x => x.ThenWait(200)
                        .Then(y =>
                        {
                            float ping = (float) (y.Timestamp - e.GetMessage().Timestamp).TotalMilliseconds;
                            return new EmbedBuilder()
                                .SetTitle("Pong - " + Environment.MachineName)
                                .SetColor(
                                    Color.Lerp(
                                        new Color(0.0f, 1.0f, 0.0f), 
                                        new Color(1.0f, 0.0f, 0.0f), 
                                        Math.Min(ping / 1000, 1f)))
                                .AddInlineField("Miki", ping.ToString("N0") + "ms")
                                .ToEmbed()
                                .EditAsync(y);
                        }));
        }

        [Command("prefix")]
		public class PrefixCommand
		{
			[Command]
			public async Task PrefixHelpAsync(IContext e)
			{
				var prefixService = e.GetService<PrefixService<IDiscordMessage>>();

				var prefix = await prefixService.GetDefaultTrigger()
					.GetForGuildAsync(
						e.GetService<MikiDbContext>(),
						e.GetService<ICacheClient>(),
						e.GetGuild().Id)
                    .ConfigureAwait(false);

				await new LocalizedEmbedBuilder(e.GetLocale())
					.WithTitle("miki_module_general_prefix_help_header")
					.WithDescription("prefix_info", prefix)
					.Build().QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
			}

			[Command("set")]
            [DefaultPermission(PermissionStatus.Deny)]
			public async Task SetPrefixAsync(IContext e)
			{
				var prefixMiddleware = e.GetService<PrefixService<IDiscordMessage>>();
				var locale = e.GetLocale();

				if(!e.GetArgumentPack().Take(out string prefix))
				{
                    return;
				}

				await prefixMiddleware.GetDefaultTrigger()
					.ChangeForGuildAsync(
						e.GetService<DbContext>(),
						e.GetService<ICacheClient>(),
						e.GetGuild().Id,
						prefix)
                    .ConfigureAwait(false);

                await new EmbedBuilder()
                    .SetTitle(locale.GetString("miki_module_general_prefix_success_header"))
                    .SetDescription(
                        locale.GetString(
                            "miki_module_general_prefix_success_message",
                            prefix))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
            }

			[Command("reset")]
            [DefaultPermission(PermissionStatus.Deny)]
			public async Task ResetPrefixAsync(IContext e)
			{
				var prefixMiddleware = e.GetService<PrefixService<IDiscordMessage>>();
				var locale = e.GetLocale();

				var trigger = prefixMiddleware.GetDefaultTrigger();

                await trigger.ChangeForGuildAsync(
                        e.GetService<DbContext>(),
                        e.GetService<ICacheClient>(),
                        e.GetGuild().Id,
                        trigger.DefaultValue)
                    .ConfigureAwait(false);

                await new EmbedBuilder()
                    .SetTitle(locale.GetString("miki_module_general_prefix_success_header"))
                    .SetDescription(
                        locale.GetString(
                            "miki_module_general_prefix_success_message",
                            trigger.DefaultValue))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
            }
		}

		[Command("stats")]
		public async Task StatsAsync(IContext e)
		{
			var cache = e.GetService<IExtendedCacheClient>();
			var locale = e.GetLocale();

            var serverCount = await cache.HashLengthAsync(CacheUtils.GuildsCacheKey)
                .ConfigureAwait(false);

            await new EmbedBuilder()
                {
                    Title = "⚙️ Miki stats",
                    Description = e.GetLocale().GetString("stats_description"),
                    Color = new Color(0.3f, 0.8f, 1),
                }.AddField(
                    $"🖥️ {locale.GetString("discord_servers")}",
                    serverCount.ToString("N0"))
                .AddField(
                    "More info",
                    "https://p.datadoghq.com/sb/01d4dd097-08d1558da4")
                .ToEmbed()
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }

		[Command("whois")]
        [GuildOnly]
        public async Task WhoIsAsync(IContext e)
		{
			IDiscordGuildUser user;
			var success = e.GetArgumentPack().Take(out string arg);
			if(success)
			{
				user = await DiscordExtensions.GetUserAsync(arg, e.GetGuild())
                    .ConfigureAwait(false);
            }
            else
            {
                if(e.GetAuthor() is IDiscordGuildUser guildUser)
                {
                    user = guildUser;
                }
                else
                {
                    throw new InvalidOperationException("Invalid author.");
                }
            }

            LocalizedEmbedBuilder embed = new LocalizedEmbedBuilder(e.GetLocale());
			embed.WithTitle(
                "whois_title", 
                user.Username + $"{(string.IsNullOrEmpty(user.Nickname) ? "" : $" ({user.Nickname})")}");
			embed.SetColor(0.5f, 0f, 1.0f);
			embed.SetThumbnail(user.GetAvatarUrl());

			var roles = await e.GetGuild()
                .GetRolesAsync()
                .ConfigureAwait(false);
            if(roles == null)
            {
                throw new ArgumentMissingException(nameof(roles));
            }

            roles = roles.Where(x => user.RoleIds.Contains(x.Id));

            Color c = roles.Where(x => x.Color != 0)
				.OrderByDescending(x => x.Position)
				.Select(x => x.Color)
				.FirstOrDefault();

			StringBuilder builder = new StringBuilder();
			builder.AppendLine($"`User Id___:` {user.Id}");
			builder.AppendLine($"`Created at:` {user.CreatedAt:dd/MM/yyyy HH:mm:ss}");
			builder.AppendLine($"`Joined at_:` {user.JoinedAt:dd/MM/yyyy HH:mm:ss}");
			builder.AppendLine($"`Color Hex_:` {c.ToString()}");

			embed.AddField(
				e.CreateResource("miki_module_whois_tag_personal"),
				new RawResource(builder.ToString())
			);

			string r = string.Join(", ", roles.Select(x => $"`{x.Name}`"));
			if(string.IsNullOrEmpty(r))
			{
				r = "none (yet!)";
			}

			embed.AddField(
				new LanguageResource("miki_module_general_guildinfo_roles"),
				new RawResource(r)
			);

			await embed.Build()
				.QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }
	}
}