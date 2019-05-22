using Microsoft.EntityFrameworkCore;
using Miki.API;
using Miki.UrbanDictionary;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Extension;
using Miki.Framework.Language;
using Miki.Helpers;
using Miki.Models;
using NCalc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Miki.Cache;
using Miki.Bot.Models;
using Miki.Dsl;
using System.Reflection;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Commands;
using Miki.Attributes;
using Miki.Discord.Rest.Exceptions;
using Miki.Framework.Arguments;
using Miki.Framework.Commands.Nodes;

namespace Miki.Modules
{
	[Module("Algemeen")]
	internal class GeneralModule
	{
   		private readonly TaskScheduler<string> taskScheduler = new TaskScheduler<string>();

		public GeneralModule()
		{
			//EventSystem.Instance.AddCommandDoneEvent(x =>
			//{
			//	x.Name = "--count-commands";
			//	x.processEvent = async (msg, e, success, t) =>
			//	{
			//		if (success)
			//		{
			//			using (var context = new MikiContext())
			//			{
			//				User user = await User.GetAsync(context, msg.Author);
			//				CommandUsage u = await CommandUsage.GetAsync(context, msg.Author.Id.ToDbLong(), e.Name);

			//				u.Amount++;
			//				user.Total_Commands++;

			//				await CommandUsage.UpdateCacheAsync(user.Id, e.Name, u);
			//				await context.SaveChangesAsync();
			//			}
			//		}
			//	};
			//});
		}

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

			int task = taskScheduler.AddTask(e.GetAuthor().Id, async (desc) =>
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
						if (reactions.Count == 0)
						{
							break;
						}

						int index = MikiRandom.Next(reactions.Count);
						winners.Add(reactions[index]);
					}

					if (updateTask != -1)
						taskScheduler.CancelReminder(e.GetAuthor().Id, updateTask);

					string winnerText = string.Join("\n", winners.Select(x => x.Username + "#" + x.Discriminator).ToArray());
					if (string.IsNullOrEmpty(winnerText))
						winnerText = "nobody!";

					await msg.EditAsync(new EditMessageArgs
					{
						embed = CreateGiveawayEmbed(e, giveawayText)
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
            IDiscordGuildUser owner = await e.GetGuild().GetOwnerAsync();

            //var emojiNames = e.GetGuild().R.Select(x => x.ToString());
            string emojiOutput = "none (yet!)";

            //if (emojiNames.Count() > 0)
            //{	
            //	emojiOutput = string.Join(",", emojiNames);
            //}
            var context = e.GetService<MikiDbContext>();

            // TODO: 
            string prefix = ">";//await e.EventSystem.GetDefaultPrefixTrigger()
                    //.GetForGuildAsync(context, e.GetService<ICacheClient>(), e.GetGuild().Id);

            var roles = await e.GetGuild().GetRolesAsync();
            var channels = await e.GetGuild().GetChannelsAsync();

            await new EmbedBuilder()
            {
                Author = new EmbedAuthor()
                {
                    Name = e.GetGuild().Name,
                    IconUrl = e.GetGuild().IconUrl,
                    Url = e.GetGuild().IconUrl
                },
            }.AddInlineField("👑 " + e.GetLocale().GetString("miki_module_general_guildinfo_owned_by"), $"{owner.Username}#{owner.Discriminator}")
            .AddInlineField("👉 " + e.GetLocale().GetString("miki_label_prefix"), prefix)
            .AddInlineField("📺 " + e.GetLocale().GetString("miki_module_general_guildinfo_channels"), channels.Count(x => x.Type == ChannelType.GUILDTEXT).ToFormattedString())
            .AddInlineField("🔊 " + e.GetLocale().GetString("miki_module_general_guildinfo_voicechannels"), channels.Count(x => x.Type == ChannelType.GUILDVOICE).ToFormattedString())
            .AddInlineField("🙎 " + e.GetLocale().GetString("miki_module_general_guildinfo_users"), roles.Count().ToFormattedString())
            .AddInlineField("#⃣ " + e.GetLocale().GetString("miki_module_general_guildinfo_roles_count"), roles.Count().ToFormattedString())
            .AddField("📜 " + e.GetLocale().GetString("miki_module_general_guildinfo_roles"),
                string.Join(",", roles.Select(x => $"`{x.Name}`")))
            .AddField("😃 " + e.GetLocale().GetString("term_emoji"), emojiOutput)
            .ToEmbed().QueueAsync(e.GetChannel());
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

            await embedBuilder.ToEmbed()
                .QueueAsync(await e.GetAuthor().GetDMChannelAsync(), "Hoi ik ben **Senko**! :wave:\n\nEn ik ben hier om jou te helpen!\nOnderaan heb ik een lijstje voor jou klaar staan met alles wat ik kan! \n\n**:scroll: Informatie**\n\nTerwijl dat jij van mijn commando's geniet zijn er enkele dingetjes die ik kan\nwaar dat jij misschien nog niets van af wist.\n\n• Senko word constant geupdate en verbeterd!\n• NSFW commando's kunnen alleen in NSFW channels worden gebreukt.\n• Je kunt altijd aan de staff vragen om nieuwe commando's toe te voegen\n• Senko zal altijd 24/7 tot jou dienst zijn.\n\n**:link: Linkjes** \n\n• Discord: <https://discord.gg/bAzdjdp>\n• Facebook: <https://bit.ly/AnimeFansNederlandFacebook> \n\n**:sparkling_heart: Verdere hulp**\n\nStiekem ben ik eigenlijk het kleine zusje van de bot Miki!\nJoin Miki's support server voor verdere hulp: <https://bit.ly/MikiSupport>")
                .Then(msg =>
                {
                    var embed = new EmbedBuilder
                        {
                            Description = e.GetLocale().GetString("miki_module_general_help_dm"),
                            Color = new Color(0.6f, 0.6f, 1.0f)
                        }
                        .ToEmbed();

                    return embed.QueueAsync(e.GetChannel());
                })
                .Catch(args =>
                {
                    // The bot doesn't have access to the DMs because the author didn't add 2FA or the user blocks DMs.
                    // Send it in the channel instead.

                    args.LogException = false;

                    return args.Reference.RequeueAsync(e.GetChannel());
                });
        }

        [Command("donate")]
		public async Task DonateAsync(IContext e)
		{
            await new EmbedBuilder()
			{
				Title = "Hoi allemaal!",
				Description = e.GetLocale().GetString("miki_module_general_info_donate_string"),
				Color = new Color(0.8f, 0.4f, 0.4f),
				ThumbnailUrl = "https://trello-attachments.s3.amazonaws.com/57acf354029527926a15e83d/598763ed8a7735cb8b52cd72/1d168f6025e40b9c6b53c3d4b8e07ccf/xdmemes.png",
			}.AddField("Linkjes", "https://www.patreon.com/mikibot - als je elke maand zou willen doneren en coole prijzen zou willen!\nhttps://ko-fi.com/velddy - Voor een eenmalige donatie!", true)
			.AddField("Heb je niet zoveel geld?", "Je kunt ons ook op andere manieren supporteren! Doe alsjeblieft mee aan onze [VoteIdeeën](https://suggestions.miki.ai/) Zodat wij een betere grip kunnen krijgen van wat jullie willen! Of vote voor Miki op [Discordbots.org](https://discordbots.org/bot/160105994217586689)", true)
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
            await new LocalizedEmbedBuilder(e.GetLocale())
				  .WithTitle(new RawResource("Ping"))
				  .WithDescription("ping_placeholder")
				  .Build()
				  .QueueAsync(e.GetChannel())
				  .ThenWait(200)
				  .Then(async x =>
				  {
					  float ping = (float)(x.Timestamp - e.GetMessage().Timestamp).TotalMilliseconds;
					  DiscordEmbed embed = new EmbedBuilder()
						  .SetTitle("Pong - " + Environment.MachineName)
						  .SetColor(Color.Lerp(new Color(0.0f, 1.0f, 0.0f), new Color(1.0f, 0.0f, 0.0f), Math.Min(ping / 1000, 1f)))
						  .AddInlineField("Miki", ((int)ping).ToFormattedString() + "ms").ToEmbed();

                      await embed.EditAsync(x);
				  });
		}

        [Command("prefix")]
        class PrefixCommand
        {
            [Command]
            public async Task PrefixHelpAsync(IContext e)
            {
                var prefixMiddleware = e.GetService<PipelineStageTrigger>();

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
                var prefixMiddleware = e.GetService<PipelineStageTrigger>();
                
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
                    .SetTitle(e.GetLocale().GetString("miki_module_general_prefix_success_header"))
                    .SetDescription(
                    e.GetLocale().GetString("miki_module_general_prefix_success_message", prefix
                )).ToEmbed()
                .QueueAsync(e.GetChannel());
            }
        }

		[Command("stats")]
		public async Task StatsAsync(IContext e)
		{
			await Task.Yield();

			var cache = MikiApp.Instance.Discord.CacheClient;

            await new EmbedBuilder()
			{
				Title = "⚙️ Miki stats",
				Description = e.GetLocale().GetString("stats_description"),
				Color = new Color(0.3f, 0.8f, 1),
			}.AddField($"🖥️ {e.GetLocale().GetString("discord_servers")}", (await cache.HashLengthAsync(CacheUtils.GuildsCacheKey)).ToFormattedString())
			 .AddField("More info", "https://p.datadoghq.com/sb/01d4dd097-08d1558da4")
			 .ToEmbed().QueueAsync(e.GetChannel());
		}

		[Command("whois")]
		public async Task WhoIsAsync(IContext e)
		{
			if (!e.GetArgumentPack().Take(out string arg))
			{
				throw new ArgumentNullException();
			}

			IDiscordGuildUser user = await DiscordExtensions.GetUserAsync(arg, e.GetGuild());

            //if (user == null)
            //{
            //	user = e.GetAuthor() as IGuildUser;
            //}

            var embed = new LocalizedEmbedBuilder(e.GetLocale());
			embed.WithTitle("whois_title", user.Username);
			embed.SetColor(0.5f, 0f, 1.0f);

			embed.SetImage(user.GetAvatarUrl());

			var roles = (await e.GetGuild().GetRolesAsync()).Where(x => user.RoleIds?.Contains(x.Id) ?? false && x.Color.Value != 0).OrderByDescending(x => x.Position);

			Color c = roles.FirstOrDefault()?.Color ?? new Color(0);

			StringBuilder builder = new StringBuilder();
			builder.AppendLine($"User Id      : **{user.Id}**");
			builder.AppendLine(
				$"Username: **{user.Username}#{user.Discriminator} {(string.IsNullOrEmpty((user as IDiscordGuildUser).Nickname) ? "" : $"({(user as IDiscordGuildUser).Nickname})")}**");
			builder.AppendLine($"Created at: **{user.CreatedAt.ToString()}**");
			builder.AppendLine($"Joined at   : **{user.JoinedAt.ToString()}**");
			builder.AppendLine($"Color Hex : **{c.ToString()}**");

			embed.AddField(
				e.CreateResource("miki_module_whois_tag_personal"),
				new RawResource(builder.ToString())
			);

			string r = string.Join(" ", roles.Select(x => x.Name));

			if (string.IsNullOrEmpty(r))
			{
				r = "none (yet!)";
			}

			if (r.Length <= 1000)
			{
				embed.AddField(
					e.CreateResource("miki_module_general_guildinfo_roles"),
					new RawResource(r)
				);
			}

            await embed.Build().QueueAsync(e.GetChannel());
		}
	}
}