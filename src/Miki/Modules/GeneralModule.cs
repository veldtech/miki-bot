using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Miki.Attributes;
using Miki.Bot.Models.Exceptions;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Dsl;
using Miki.Framework;
using Miki.Framework.Arguments;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Nodes;
using Miki.Framework.Commands.Permissions;
using Miki.Framework.Commands.Permissions.Attributes;
using Miki.Framework.Commands.Permissions.Models;
using Miki.Framework.Commands.Prefixes;
using Miki.Framework.Commands.Prefixes.Triggers;
using Miki.Framework.Commands.Scopes;
using Miki.Framework.Commands.Scopes.Attributes;
using Miki.Functional;
using Miki.Functional.Async;
using Miki.Helpers;
using Miki.Localization;
using Miki.Localization.Exceptions;
using Miki.Modules.Accounts.Services;
using Miki.Services;
using Miki.Services.Achievements;
using Miki.Utility;
using NCalc;

namespace Miki.Modules
{
    [Module("General"), Emoji(AppProps.Emoji.SpeechBubble)]
	public class GeneralModule
	{
        private static class FeatureFlags
        {
            public const string GiveawayUsingNewScheduler = "giveaway_using_new_scheduler";
        }

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
                    var user = await e.GetGuild().FindUserAsync(arg).ConfigureAwait(false);
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
            if(string.IsNullOrWhiteSpace(expressionString))
            {
                throw new ArgumentMissingException("entity_expression");
            }

            expressionString = expressionString.Trim('\'');

            var expression = new Expression(expressionString, EvaluateOptions.NoCache);
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
                        x.Result = n * (1.0 - o) + v * o;
                        break;
                    }
                }
            };

            var result = Result<string>.From(() => expression.Evaluate().ToString());
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
            if(!e.HasFeatureEnabled(FeatureFlags.GiveawayUsingNewScheduler))
            {
                throw new CommandDisabledException();
            }

            DiscordEmoji.TryParse("🎁", out var emoji);

            var args = e.GetArgumentPack();

            var giveawayText = args.TakeRequired<string>();
            if(args.CanTake)
            {
                while(!args.Pack.Peek().HasValue)
                {
                    giveawayText += " " + args.Pack.Take();
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

            giveawayText += amount > 1 ? $" x {amount:N0}" : "";

            List<IDiscordUser> winners = new List<IDiscordUser>();

            IDiscordMessage msg = await CreateGiveawayEmbed(e, giveawayText)
                .AddField("Time", timeLeft.ToTimeString(e.GetLocale()), true)
                .AddField("React to participate", "good luck", true)
                .ToEmbed()
                .SendToChannelAsync(e.GetChannel())
                .ConfigureAwait(false);

            await msg.CreateReactionAsync(emoji)
                .ConfigureAwait(false);

            //taskScheduler.AddTask(e.GetAuthor().Id, async (desc) =>
            //{
            //    msg = await e.GetChannel()
            //        .GetMessageAsync(msg.Id)
            //        .ConfigureAwait(false);
            //    if(msg == null)
            //    {
            //        return;
            //    }

            //    await msg.DeleteReactionAsync(emoji)
            //        .ConfigureAwait(false);

            //    await Task.Delay(1000)
            //        .ConfigureAwait(false);

            //    var reactions = (await msg.GetReactionsAsync(emoji)
            //            .ConfigureAwait(false))
            //        .ToList();

            //    //do
            //    //{
            //    //	reactions.AddRange();
            //    //	reactionsGained += 100;
            //    //} while (reactions.Count == reactionsGained);

            //    // Select random winners
            //    for(int i = 0; i < amount; i++)
            //    {
            //        if(!reactions.Any())
            //        {
            //            break;
            //        }

            //        int index = MikiRandom.Next(reactions.Count());
            //        winners.Add(reactions.ElementAtOrDefault(index));
            //    }

            //    if(updateTask != -1)
            //    {
            //        taskScheduler.CancelReminder(e.GetAuthor().Id, updateTask);
            //    }

            //    string winnerText = string.Join(
            //        "\n", winners.Select(x => $"{x.Username}#{x.Discriminator}").ToArray());
            //    if(string.IsNullOrEmpty(winnerText))
            //    {
            //        winnerText = "nobody!";
            //    }

            //    await msg.EditAsync(new EditMessageArgs
            //    {
            //        Embed = CreateGiveawayEmbed(e, giveawayText)
            //            .AddField("Winners", winnerText)
            //            .ToEmbed()
            //    }).ConfigureAwait(false);
            //}, "description var", timeLeft);
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

            var context = e.GetService<IUnitOfWork>();
			var cache = e.GetService<ICacheClient>();

            if(!(e.GetService<IPrefixService>()
                .GetDefaultTrigger() is DynamicPrefixTrigger trigger))
            {
                throw new InvalidOperationException("Cannot get default trigger");
            }

            var prefix = await trigger.GetForGuildAsync(context, cache, guild.Id)
                .ConfigureAwait(false);

            var roles = (await e.GetGuild().GetRolesAsync().ConfigureAwait(false)).ToList();
            var channels = (await e.GetGuild().GetChannelsAsync().ConfigureAwait(false)).ToList();
            var rolesString = string.Join(",", roles.Select(x => $"`{x.Name}`"));

            await new EmbedBuilder()
                .SetAuthor(guild.Name, guild.IconUrl, guild.IconUrl)
                .AddInlineField(
                    $"👑 {locale.GetString("miki_module_general_guildinfo_owned_by")}",
				    $"{owner.Username}#{owner.Discriminator}")
                .AddInlineField(
                    $"👉 {locale.GetString("miki_label_prefix")}", prefix)
                .AddInlineField(
                    $"📺 {locale.GetString("miki_module_general_guildinfo_channels")}",
				    channels.Count(x => x.Type == ChannelType.GuildText).ToString("N0"))
                .AddInlineField(
                    $"🔊 {locale.GetString("miki_module_general_guildinfo_voicechannels")}",
				    channels.Count(x => x.Type == ChannelType.GuildVoice).ToString("N0"))
                .AddInlineField(
                    $"🙎 {locale.GetString("miki_module_general_guildinfo_users")}",
				    roles.Count.ToString("N0"))
                .AddInlineField(
                    $"#⃣ {locale.GetString("miki_module_general_guildinfo_roles_count")}",
				    roles.Count.ToString("N0"))
                .AddField(
                    $"📜 {locale.GetString("miki_module_general_guildinfo_roles")}",
                    rolesString.SplitStringUntil(",", 1500))
                .ToEmbed()
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }

        [Command("help")]
        public class HelpCommand
        {
            [Command]
            public async Task HelpAsync(IContext e)
            {
                var locale = e.GetLocale();
                var commandHandler = e.GetService<CommandTreeService>();

                var embedBuilder = new EmbedBuilder()
                    .SetColor(0.6f, 0.6f, 1.0f);

                var arg = e.GetArgumentPack().Pack.TakeAll();
                if (!string.IsNullOrWhiteSpace(arg))
                {
                    if (commandHandler.DoesModuleExist(arg))
                    {
                        var moduleNode = commandHandler.GetModuleByName(arg);

                        embedBuilder.SetTitle(
                            moduleNode.Metadata.Identifiers.FirstOrDefault()?.CapitalizeFirst() 
                                ?? locale.GetString("miki_placeholder_null"));

                        await foreach (var module in moduleNode.GetAllExecutableAsync(e))
                        {
                            var name = module.Metadata.Identifiers.First();

                            string description;
                            try
                            {
                                description = locale.GetString(
                                    $"miki_command_description_{name.Replace(' ', '_').ToLowerInvariant()}");
                            }
                            catch
                            {
                                description = locale.GetString("miki_error_message_command_description");
                            }

                            embedBuilder.AddInlineField($"{e.GetPrefixMatch()}{name}", description);
                        }

                        await embedBuilder
                            .ToEmbed()
                            .QueueAsync(e, e.GetChannel())
                            .ConfigureAwait(false);
                    }
                    else if (commandHandler.DoesCommandExist(arg))
                    {
                        var commandNode = commandHandler.GetCommandByName(arg);

                        var name = commandNode.Metadata.Identifiers.First();

                        embedBuilder.SetTitle(
                            name?.CapitalizeFirst() ??
                            locale.GetString("miki_placeholder_null")
                        );

                        string description;
                        try
                        {
                            description = locale.GetString($"miki_command_description_{name.ToLowerInvariant()}");
                        }
                        catch
                        {
                            description = locale.GetString("miki_error_message_generic");
                        }

                        embedBuilder.SetDescription(description);

                        string usage;
                        try
                        {
                            usage = locale.GetString($"miki_command_usage_{name.ToLowerInvariant()}");
                        }
                        catch
                        {
                            usage = locale.GetString("miki_error_message_generic");
                        }

                        await embedBuilder.AddInlineField(locale.GetString("miki_module_general_help_usage"), usage)
                            .ToEmbed()
                            .QueueAsync(e, e.GetChannel())
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await embedBuilder
                            .SetColor(255, 0, 0)
                            .SetTitle(locale.GetString("miki_module_help_error_null_header"))
                            .SetDescription(locale.GetString("error_command_null", arg))
                            .ToEmbed()
                            .QueueAsync(e, e.GetChannel())
                            .ConfigureAwait(false);
                    }
                }
                else
                {
                    await new EmbedBuilder()
                        .SetDescription(locale.GetString("miki_module_general_help_dm"))
                        .SetColor(0.6f, 0.6f, 1.0f)
                        .ToEmbed()
                        .QueueAsync(e, e.GetChannel())
                        .ConfigureAwait(false);

                    await BuildNodeListAsync(e, embedBuilder);
                    
                    var dmChannel = await e.GetAuthor().GetDMChannelAsync();
                    await embedBuilder
                        .SetTitle(new IconResource("📚", "miki_module_general_help_header").Get(locale))
                        .SetDescription(locale.GetString("miki_module_general_help_tagline"))
                        .SetFooter(locale.GetString(
                            "miki_module_general_help_module_example",
                            e.GetPrefixMatch()
                        ))
                        .ToEmbed()
                        .QueueAsync(e, dmChannel, "Join our support server: https://discord.gg/39Xpj7K")
                        .ConfigureAwait(false);
                }
            }

            private async Task BuildNodeListAsync(IContext context, EmbedBuilder builder)
            {
                var locale = context.GetLocale();
                var scopesService = context.GetService<IScopeService>();
                var commandHandler = context.GetService<CommandTreeService>();

                foreach (var nodeModule in commandHandler.GetModules())
                {
                    var nodes = new List<Node>();
                    
                    await foreach (var node in nodeModule.GetAllExecutableAsync(context))
                    {
                        var scopeIds = node.Attributes
                            .OfType<RequiresScopeAttribute>()
                            .Select(x => x.ScopeId)
                            .ToList();
                        if (!await scopesService.HasScopeAsync(
                            (long)context.GetAuthor().Id,scopeIds))
                        {
                            continue;
                        }

                        nodes.Add(node);
                    }

                    if (!nodes.Any())
                    {
                        continue;
                    }
                    
                    var id = nodeModule.Metadata.Identifiers.First();
                    var emoji = nodeModule.Attributes.OfType<EmojiAttribute>().FirstOrDefault();
                    string description;

                    try
                    {
                        description = locale.GetString($"miki_module_{id.ToLowerInvariant()}_description");
                    }
                    catch
                    {
                        description = locale.GetString("miki_error_message_generic");
                    }

                    if (string.IsNullOrEmpty(description))
                    {
                        description = locale.GetString("miki_placeholder_null");
                    }

                    builder.AddInlineField($"{emoji?.Emoji}  {id.CapitalizeFirst()}", description);
                }
            }
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

            embed.AddField(e.GetLocale().GetString("miki_module_general_info_links"),
				$"`{e.GetLocale().GetString("miki_module_general_info_docs"),-15}:` [documentation](https://www.github.com/velddev/miki/wiki)\n" +
				$"`{"Donate",-15}:` [patreon](https://www.patreon.com/mikibot) | [ko-fi](https://ko-fi.com/velddy)\n" +
				$"`{e.GetLocale().GetString("miki_module_general_info_twitter"),-15}:` [veld](https://www.twitter.com/velddev) | [miki](https://www.twitter.com/miki_discord)\n" +
				$"`{e.GetLocale().GetString("miki_module_general_info_reddit"),-15}:` [/r/mikibot](https://www.reddit.com/r/mikibot) \n" +
				$"`{e.GetLocale().GetString("miki_module_general_info_server"),-15}:` [discord](https://discord.gg/39Xpj7K)\n" +
				$"`{e.GetLocale().GetString("miki_module_general_info_website"),-15}:` [link](https://miki.ai) | [suggestions](https://suggestions.miki.ai/) | [guides](https://miki.ai/guides)");

			await embed.ToEmbed()
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);

            var service = e.GetService<AchievementService>();
            await service.UnlockAsync(
                service.GetAchievement(AchievementIds.ReadInfoId), e.GetAuthor().Id);
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
                + "\n" + AppProps.Links.DiscordInvite);
        }

        [Command("ping", "lag")]
        public Task PingAsync(IContext e)
        {
            var locale = e.GetLocale();
            return new EmbedBuilder()
                .SetTitle("Ping")
                .SetDescription(locale.GetString("ping_placeholder"))
                .ToEmbed()
                .QueueAsync(e, e.GetChannel(), modifier: x => x.ThenWait(200)
                    .Then(message =>
                    {
                        float ping = (float)(message.Timestamp - e.GetMessage().Timestamp)
                            .TotalMilliseconds;
                        return new EmbedBuilder()
                            .SetTitle($"Pong - {Environment.MachineName}")
                            .SetColor(Color.Lerp(
                                new Color(0, 255, 0), new Color(255, 0, 0), Math.Min(ping / 1000, 1f)))
                            .AddInlineField("Miki", $"{ping:N0}ms")
                            .ToEmbed()
                            .EditAsync(message);
                    }));
        }

        [Command("prefix")]
		public class PrefixCommand
		{
			[Command]
			public async Task PrefixHelpAsync(IContext e)
			{
				var prefixService = e.GetService<IPrefixService>();
                var locale = e.GetLocale();

				var prefix = await (prefixService.GetDefaultTrigger() as DynamicPrefixTrigger)
					.GetForGuildAsync(
						e.GetService<IUnitOfWork>(),
						e.GetService<ICacheClient>(),
						e.GetGuild().Id)
                    .ConfigureAwait(false);

                await new EmbedBuilder()
					.SetTitle(locale.GetString("miki_module_general_prefix_help_header"))
					.SetDescription(locale.GetString("prefix_info", prefix))
					.ToEmbed()
                    .QueueAsync(e, e.GetChannel())
                    .ConfigureAwait(false);
			}

			[Command("set")]
            [DefaultPermission(PermissionStatus.Deny)]
			public async Task SetPrefixAsync(IContext e)
			{
				var prefixMiddleware = e.GetService<IPrefixService>();
				var locale = e.GetLocale();

				if(!e.GetArgumentPack().Take(out string prefix))
				{
                    return;
				}

				await (prefixMiddleware.GetDefaultTrigger() as DynamicPrefixTrigger)
					.ChangeForGuildAsync(
						e.GetService<IUnitOfWork>(),
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
				var prefixMiddleware = e.GetService<IPrefixService>();
				var locale = e.GetLocale();

				var trigger = prefixMiddleware.GetDefaultTrigger() as DynamicPrefixTrigger;

                await trigger.ChangeForGuildAsync(
                        e.GetService<IUnitOfWork>(),
                        e.GetService<ICacheClient>(),
                        e.GetGuild().Id,
                        trigger.DefaultValue)
                    .ConfigureAwait(false);

                await new EmbedBuilder()
                    .SetTitle(locale.GetString("miki_module_general_prefix_success_header"))
                    .SetDescription(
                        locale.GetString("miki_module_general_prefix_success_message", trigger.DefaultValue))
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

            var serverCount = await cache.HashLengthAsync("discord:guilds")
                .ConfigureAwait(false);

            await new EmbedBuilder()
                .SetTitle("⚙️ Miki stats")
                .SetDescription(locale.GetString("stats_description"))
                .SetColor(0.3f, 0.8f, 1)
                .AddField($"🖥️ {locale.GetString("discord_servers")}", serverCount.ToString("N0"))
                .AddField("More info", "https://p.datadoghq.com/sb/01d4dd097-08d1558da4")
                .ToEmbed()
                .QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }

		[Command("whois")]
        [GuildOnly]
        public async Task WhoIsAsync(IContext e)
		{
			IDiscordGuildUser user;
			if(e.GetArgumentPack().Take(out string arg))
			{
				user = await e.GetGuild().FindUserAsync(arg)
                    .ConfigureAwait(false);
            }
            else
            {
                if(!(e.GetAuthor() is IDiscordGuildUser guildUser))
                {
                    throw new InvalidOperationException("Invalid author.");
                }
                user = guildUser;
            }

            var locale = e.GetLocale();

            var embed = new EmbedBuilder()
                .SetTitle(locale.GetString("whois_title", GetWhoIsUsername(user)))
                .SetColor(0.5f, 0f, 1.0f)
                .SetThumbnail(user.GetAvatarUrl());

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"`User Id___:` {user.Id}");
            builder.AppendLine($"`Created at:` {user.CreatedAt:dd/MM/yyyy HH:mm:ss}");
            builder.AppendLine($"`Joined at_:` {user.JoinedAt:dd/MM/yyyy HH:mm:ss}");

            var roles = await e.GetGuild()
                .GetRolesAsync()
                .ConfigureAwait(false);
            if(roles != null)
            {
                roles = roles.Where(x => user.RoleIds.Contains(x.Id));

                Color c = roles.Where(x => x.Color != 0)
                    .OrderByDescending(x => x.Position)
                    .Select(x => x.Color)
                    .FirstOrDefault();
                builder.AppendLine($"`Color Hex_:` {c}");

                string r = string.Join(", ", roles.Select(x => $"`{x.Name}`"));
                if(string.IsNullOrEmpty(r))
                {
                    r = "none (yet!)";
                }

                embed.AddField(locale.GetString("miki_module_general_guildinfo_roles"), r);
            }

			embed.AddField(locale.GetString("miki_module_whois_tag_personal"), builder.ToString());

            await embed.ToEmbed()
				.QueueAsync(e, e.GetChannel())
                .ConfigureAwait(false);
        }

        private string GetWhoIsUsername(IDiscordGuildUser user)
        {
            if(string.IsNullOrEmpty(user.Nickname))
            {
                return user.Username;
            }
            return user.Username + $" ({user.Nickname})";
        }
	}
}
