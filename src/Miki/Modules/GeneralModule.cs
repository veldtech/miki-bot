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
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Commands.Nodes;
using Miki.Framework.Extension;
using Miki.Framework.Language;
using Miki.Helpers;
using NCalc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module("General")]
	internal class GeneralModule
	{
   		private readonly TaskScheduler<string> _taskScheduler = new TaskScheduler<string>();

		[Command("avatar")]
		public async Task AvatarAsync(IContext e)
		{
			if (!e.GetArgumentPack().Take(out string arg))
			{
				e.GetChannel().QueueMessage(e.GetAuthor().GetAvatarUrl());
			}
			else
			{
				if (arg == "-s")
				{
					e.GetChannel().QueueMessage(e.GetGuild().IconUrl);
					return;
				}

				IDiscordGuildUser user = await DiscordExtensions.GetUserAsync(arg, e.GetGuild());
				if (user != null)
				{
					e.GetChannel().QueueMessage(user.GetAvatarUrl());
				}
			}
		}

		[Command("calc", "calculate")]
		public Task CalculateAsync(IContext e)
		{
			try
			{
				Expression expression = new Expression(e.GetArgumentPack().Pack.TakeAll());

				expression.Parameters.Add("pi", Math.PI);

				expression.EvaluateFunction += (name, x) =>
				{
					if (name == "lerp")
					{
						double n = (double)x.Parameters[0].Evaluate();
						double v = (double)x.Parameters[1].Evaluate();
						double o = (double)x.Parameters[2].Evaluate();
						x.Result = (n * (1.0 - o)) + (v * o);
					}
				};

				object output = expression.Evaluate();

				e.GetChannel().QueueMessage(output.ToString());
			}
			catch (Exception ex)
			{
				e.GetChannel().QueueMessage(e.GetLocale().GetString("miki_module_general_calc_error") + "\n```" + ex.Message + "```");
			}
			return Task.CompletedTask;
		}

		[Command("changelog")]
		public async Task ChangelogAsync(IContext e)
		{
			await Task.Yield();
            await new EmbedBuilder()
			{
				Title = "Changelog",
				Description = "Check out my changelog blog [here](https://blog.miki.ai/)!"
			}.ToEmbed().QueueAsync(e.GetChannel());
		}

		[Command("giveaway")]
		public async Task GiveawayAsync(IContext e)
		{
			DiscordEmoji emoji = new DiscordEmoji();
			emoji.Name = "🎁";

            e.GetArgumentPack().Take(out string giveawayText);

			while (!e.GetArgumentPack().Pack.Peek().StartsWith("-"))
			{
                giveawayText += " " + e.GetArgumentPack().Pack.Take();
			}

			var mml = new MMLParser(e.GetArgumentPack().Pack.TakeAll()).Parse();

			int amount = mml.Get("amount", 1);
			TimeSpan timeLeft = mml.Get("time", "1h").GetTimeFromString();

			if (amount > 10)
			{
                await e.ErrorEmbed("We can only allow up to 10 picks per giveaway")
					.ToEmbed().QueueAsync(e.GetChannel());
				return;
			}

			giveawayText += ((amount > 1) ? " x " + amount : "");

			List<IDiscordUser> winners = new List<IDiscordUser>();

			IDiscordMessage msg = await CreateGiveawayEmbed(e, giveawayText)
			.AddField("Time", timeLeft.ToTimeString(e.GetLocale()), true)
			.AddField("React to participate", "good luck", true)
			.ToEmbed()
            .SendToChannel(e.GetChannel());

			await msg.CreateReactionAsync(emoji);

			int updateTask = -1;

			int task = _taskScheduler.AddTask(e.GetAuthor().Id, async (desc) =>
			{
				msg = await e.GetChannel().GetMessageAsync(msg.Id);

				if (msg != null)
				{
					await msg.DeleteReactionAsync(emoji);

                    await Task.Delay(1000);

					var reactions = await msg.GetReactionsAsync(emoji);

					//do
					//{
					//	reactions.AddRange();
					//	reactionsGained += 100;
					//} while (reactions.Count == reactionsGained);

					// Select random winners
					for (int i = 0; i < amount; i++)
					{
						if (reactions.Count() == 0)
						{
							break;
						}

						int index = MikiRandom.Next(reactions.Count());
						winners.Add(reactions.ElementAtOrDefault(index));
					}

					if (updateTask != -1)
						_taskScheduler.CancelReminder(e.GetAuthor().Id, updateTask);

					string winnerText = string.Join("\n", winners.Select(x => x.Username + "#" + x.Discriminator).ToArray());
					if (string.IsNullOrEmpty(winnerText))
						winnerText = "nobody!";

					await msg.EditAsync(new EditMessageArgs
					{
						Embed = CreateGiveawayEmbed(e, giveawayText)
							.AddField("Winners", winnerText)
							.ToEmbed()
					});
				}
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
        public async Task GuildInfoAsync(IContext e)
        {
            var guild = e.GetGuild();
            var locale = e.GetLocale();

            IDiscordGuildUser owner = await guild.GetOwnerAsync();

            string emojiOutput = "none (yet!)";
            //if (guild.getemoji.Count() > 0)
            //{
            //    emojiOutput = string.Join(",", emojiNames);
            //}

            var context = e.GetService<MikiDbContext>();
            var cache = e.GetService<ICacheClient>();

            string prefix = await e.GetStage<PipelineStageTrigger>()
                .GetDefaultTrigger()
                .GetForGuildAsync(context, cache, guild.Id);

            var roles = await e.GetGuild().GetRolesAsync();
            var channels = await e.GetGuild().GetChannelsAsync();

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
                channels.Count(x => x.Type == ChannelType.GUILDTEXT).ToFormattedString());

            builder.AddInlineField(
                "🔊 " + locale.GetString("miki_module_general_guildinfo_voicechannels"), 
                channels.Count(x => x.Type == ChannelType.GUILDVOICE).ToFormattedString());

            builder.AddInlineField(
                "🙎 " + locale.GetString("miki_module_general_guildinfo_users"), 
                roles.Count().ToFormattedString());

            builder.AddInlineField(
                "#⃣ " + locale.GetString("miki_module_general_guildinfo_roles_count"), 
                roles.Count().ToFormattedString());

            builder.AddField(
                "📜 " + locale.GetString("miki_module_general_guildinfo_roles"),
                string.Join(",", roles.Select(x => $"`{x.Name}`")));

            builder.AddField(
                "😃 " + e.GetLocale().GetString("term_emoji"), 
                emojiOutput);

            await builder.ToEmbed()
                .QueueAsync(e.GetChannel());
        }

        [Command("help")]
        public async Task HelpAsync(IContext e)
        {
            var commandTree = e.GetService<CommandTree>();

            if (e.GetArgumentPack().Take(out string arg))
            {
                var command = commandTree.GetCommand(new ArgumentPack(arg.Split(' ')));
                string prefix = ">"; // TODO

                if (command == null)
                {
                    var helpListEmbed = new EmbedBuilder();
                    helpListEmbed.Title = e.GetLocale().GetString("miki_module_help_error_null_header");
                    helpListEmbed.Description = e.GetLocale().GetString("miki_module_help_error_null_message", prefix);
                    helpListEmbed.Color = new Color(0.6f, 0.6f, 1.0f);

                    var allExecutables = await commandTree.Root.GetAllExecutableAsync(e);
                    var comparer = new API.StringComparison.StringComparer(allExecutables.SelectMany(node => node.Metadata.Identifiers));
                    var best = comparer.GetBest(arg);

                    helpListEmbed.AddField(e.GetLocale().GetString("miki_module_help_didyoumean"), best.text);

                    await helpListEmbed.ToEmbed()
                        .QueueAsync(e.GetChannel());
                }
                else
                {
                    if (!(await command.ValidateRequirementsAsync(e)))
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
                        e.GetLocale().GetString("miki_module_general_help_description"),
                        e.GetLocale().GetString("miki_command_description_" + commandId.ToLower()) ?? e.GetLocale().GetString("miki_placeholder_null"));

                    explainedHelpEmbed.AddField(
                        e.GetLocale().GetString("miki_module_general_help_usage"),
                        e.GetLocale().GetString("miki_command_usage_" + commandId.ToLower()) ?? e.GetLocale().GetString("miki_placeholder_null"));

                    await explainedHelpEmbed.ToEmbed().QueueAsync(e.GetChannel());
                }
                return;
            }

            await new EmbedBuilder()
            {
                Description = e.GetLocale().GetString("miki_module_general_help_dm"),
                Color = new Color(0.6f, 0.6f, 1.0f)
            }.ToEmbed().QueueAsync(e.GetChannel());

            var embedBuilder = new EmbedBuilder();

            foreach (var nodeModule in commandTree.Root.Children.OfType<NodeModule>())
            {
                var id = nodeModule.Metadata.Identifiers.First();
                var executables = await nodeModule.GetAllExecutableAsync(e);
                var commandNames = string.Join(", ", executables.Select(node => '`' + node.Metadata.Identifiers.First() + '`'));

                if (!string.IsNullOrEmpty(commandNames))
                {
                    embedBuilder.AddField(id.ToUpper(), commandNames);
                }
            }

            await embedBuilder.ToEmbed().QueueAsync(await e.GetAuthor().GetDMChannelAsync(), "Join our support server: https://discord.gg/39Xpj7K");
        }

        [Command("donate")]
		public async Task DonateAsync(IContext e)
		{
            await new EmbedBuilder()
			{
				Title = "Hi everyone!",
				Description = e.GetLocale().GetString("miki_module_general_info_donate_string"),
				Color = new Color(0.8f, 0.4f, 0.4f),
				ThumbnailUrl = "https://trello-attachments.s3.amazonaws.com/57acf354029527926a15e83d/598763ed8a7735cb8b52cd72/1d168f6025e40b9c6b53c3d4b8e07ccf/xdmemes.png",
			}.AddField("Links", "https://www.patreon.com/mikibot - if you want to donate every month and get cool rewards!\nhttps://ko-fi.com/velddy - one time donations please include your discord name#identifiers so i can contact you!", true)
			.AddField("Don't have money?", "You can always support us in different ways too! Please participate in our [idea](https://suggestions.miki.ai/) discussions so we can get a better grasp of what you guys would like to see next! Or vote for Miki on [Discordbots.org](https://discordbots.org/bot/160105994217586689)", true)
			.ToEmbed().QueueAsync(e.GetChannel());
		}

		[Command("info", "about")]
        public async Task InfoAsync(IContext e)
		{
            Version v = Assembly.GetEntryAssembly().GetName().Version;

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
				$"`{e.GetLocale().GetString("miki_module_general_info_website").PadRight(15)}:` [link](https://miki.ai) [suggestions](https://suggestions.miki.ai/)");

            await embed.ToEmbed().QueueAsync(e.GetChannel());

			await Task.Yield();
		}

		[Command("invite")]
		public async Task InviteAsync(IContext e)
		{
			e.GetChannel().QueueMessage(e.GetLocale().GetString("miki_module_general_invite_message"));
			(await e.GetAuthor().GetDMChannelAsync()).QueueMessage(e.GetLocale().GetString("miki_module_general_invite_dm")
				+ "\nhttps://discordapp.com/oauth2/authorize?&client_id=160185389313818624&scope=bot&permissions=355593334");
		}

		[Command("ping", "lag")]
		public async Task PingAsync(IContext e)
		{
            (await new LocalizedEmbedBuilder(e.GetLocale())
				  .WithTitle(new RawResource("Ping"))
				  .WithDescription("ping_placeholder")
				  .Build()
				  .QueueAsync(e.GetChannel()))
				  .ThenWait(200)
				  .Then(async x =>
				  {
					  float ping = (float)(x.Timestamp - e.GetMessage().Timestamp).TotalMilliseconds;
					  await new EmbedBuilder()
						  .SetTitle("Pong - " + Environment.MachineName)
						  .SetColor(Color.Lerp(new Color(0.0f, 1.0f, 0.0f), new Color(1.0f, 0.0f, 0.0f), Math.Min(ping / 1000, 1f)))
						  .AddInlineField("Miki", ((int)ping).ToFormattedString() + "ms")
                          .ToEmbed()
                          .EditAsync(x);
				  });
		}

        [Command("prefix")]
        class PrefixCommand
        {
            [Command]
            public async Task PrefixHelpAsync(IContext e)
            {
                var prefixMiddleware = e.GetStage<PipelineStageTrigger>();

                var prefix = await prefixMiddleware.GetDefaultTrigger()
                    .GetForGuildAsync(
                        e.GetService<MikiDbContext>(),
                        e.GetService<ICacheClient>(),
                        e.GetGuild().Id);

                await new LocalizedEmbedBuilder(e.GetLocale())
                    .WithTitle("miki_module_general_prefix_help_header")
                    .WithDescription("prefix_info", prefix)
                    .Build().QueueAsync(e.GetChannel());
            }

            [Command("set")]
            public async Task SetPrefixAsync(IContext e)
            {
                var prefixMiddleware = e.GetStage<PipelineStageTrigger>();
                var locale = e.GetLocale();

                if(!e.GetArgumentPack().Take(out string prefix))
                {

                }

                await prefixMiddleware.GetDefaultTrigger()
                    .ChangeForGuildAsync(
                        e.GetService<DbContext>(),
                        e.GetService<ICacheClient>(),
                        e.GetGuild().Id,
                        prefix);

                await new EmbedBuilder()
                    .SetTitle(locale.GetString("miki_module_general_prefix_success_header"))
                    .SetDescription(
                        locale.GetString(
                            "miki_module_general_prefix_success_message", 
                            prefix))
                    .ToEmbed()
                .QueueAsync(e.GetChannel());
            }
        }

		[Command("stats")]
		public async Task StatsAsync(IContext e)
		{
			var cache = e.GetService<IExtendedCacheClient>();
            var locale = e.GetLocale();

            await new EmbedBuilder()
			{
				Title = "⚙️ Miki stats",
				Description = e.GetLocale().GetString("stats_description"),
				Color = new Color(0.3f, 0.8f, 1),
			}.AddField(
                $"🖥️ {locale.GetString("discord_servers")}", 
                (await cache.HashLengthAsync(CacheUtils.GuildsCacheKey)).ToString("N0"))
			 .AddField(
                "More info", 
                "https://p.datadoghq.com/sb/01d4dd097-08d1558da4")
			 .ToEmbed()
             .QueueAsync(e.GetChannel());
		}

		[Command("whois")]
		public async Task WhoIsAsync(IContext e)
		{
            IDiscordGuildUser user;
            var success = e.GetArgumentPack().Take(out string arg);
            if (success)
            {
                user = await DiscordExtensions.GetUserAsync(arg, e.GetGuild());
            }
            else 
            {
                user = e.GetAuthor() as IDiscordGuildUser;
            }

            LocalizedEmbedBuilder embed = new LocalizedEmbedBuilder(e.GetLocale());
            embed.WithTitle("whois_title", user.Username + $"{(string.IsNullOrEmpty(user.Nickname) ? "" : $" ({user.Nickname})")}");
            embed.SetColor(0.5f, 0f, 1.0f);
            embed.SetThumbnail(user.GetAvatarUrl());

            var roles = await e.GetGuild().GetRolesAsync();

            Color c = roles.Where(x => x.Color != 0)
                .Where(x => user.RoleIds.Contains(x.Id))
                .OrderByDescending(x => x.Position)
                .Select(x => x.Color)
                .FirstOrDefault() ?? new Color(0);

			StringBuilder builder = new StringBuilder();
			builder.AppendLine($"`User Id___:` {user.Id}");
			builder.AppendLine($"`Created at:` {user.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")}");
			builder.AppendLine($"`Joined at_:` {user.JoinedAt.ToString("dd/MM/yyyy HH:mm:ss")}");
			builder.AppendLine($"`Color Hex_:` {c.ToString()}");

			embed.AddField(
				e.CreateResource("miki_module_whois_tag_personal"),
				new RawResource(builder.ToString())
			);

			string r = string.Join(", ", roles
                .Select(x => $"`{x.Name}`"));
            if (string.IsNullOrEmpty(r))
			{
				r = "none (yet!)";
			}

            embed.AddField(
                e.CreateResource("miki_module_general_guildinfo_roles"),
                new RawResource(r)
            );

            await embed.Build()
                .QueueAsync(e.GetChannel());
		}
	}
}