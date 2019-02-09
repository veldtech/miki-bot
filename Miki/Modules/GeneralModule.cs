using Microsoft.EntityFrameworkCore;
using Miki.API;
using Miki.UrbanDictionary;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Dsl;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
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

namespace Miki.Modules
{
	[Module("General")]
	internal class GeneralModule
	{
   		private readonly TaskScheduler<string> taskScheduler = new TaskScheduler<string>();

		public GeneralModule(Module m, MikiApp b)
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

		[Command(Name = "avatar")]
		public async Task AvatarAsync(EventContext e)
		{
			if (!e.Arguments.Take(out string arg))
			{
				e.Channel.QueueMessage(e.Author.GetAvatarUrl());
			}
			else
			{
				if (arg == "-s")
				{
					e.Channel.QueueMessage(e.Guild.IconUrl);
					return;
				}

				IDiscordGuildUser user = await DiscordExtensions.GetUserAsync(arg, e.Guild);
				if (user != null)
				{
					e.Channel.QueueMessage(user.GetAvatarUrl());
				}
			}
		}

		[Command(Name = "calc", Aliases = new string[] { "calculate" })]
		public Task CalculateAsync(EventContext e)
		{
			try
			{
				Expression expression = new Expression(e.Arguments.Pack.TakeAll());

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

				e.Channel.QueueMessage(output.ToString());
			}
			catch (Exception ex)
			{
				e.Channel.QueueMessage(e.Locale.GetString("miki_module_general_calc_error") + "\n```" + ex.Message + "```");
			}
			return Task.CompletedTask;
		}

		[Command(Name = "changelog")]
		public async Task ChangelogAsync(EventContext e)
		{
			await Task.Yield();
            await new EmbedBuilder()
			{
				Title = "Changelog",
				Description = "Check out my changelog blog [here](https://blog.miki.ai/)!"
			}.ToEmbed().QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "giveaway")]
		public async Task GiveawayAsync(EventContext e)
		{
			DiscordEmoji emoji = new DiscordEmoji();
			emoji.Name = "🎁";

            e.Arguments.Take(out string giveawayText);

			while (!e.Arguments.Pack.Peek().StartsWith("-"))
			{
                giveawayText += " " + e.Arguments.Pack.Take();
			}

			var mml = new MMLParser(e.Arguments.Pack.TakeAll()).Parse();

			int amount = mml.Get("amount", 1);
			TimeSpan timeLeft = mml.Get("time", "1h").GetTimeFromString();

			if (amount > 10)
			{
                await e.ErrorEmbed("We can only allow up to 10 picks per giveaway")
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

			giveawayText += ((amount > 1) ? " x " + amount : "");

			List<IDiscordUser> winners = new List<IDiscordUser>();

			IDiscordMessage msg = await CreateGiveawayEmbed(e, giveawayText)
			.AddField("Time", timeLeft.ToTimeString(e.Locale), true)
			.AddField("React to participate", "good luck", true)
			.ToEmbed().SendToChannel(e.Channel);

			await msg.CreateReactionAsync(emoji);

			int updateTask = -1;

			int task = taskScheduler.AddTask(e.Author.Id, async (desc) =>
			{
				msg = await e.Channel.GetMessageAsync(msg.Id);

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
						taskScheduler.CancelReminder(e.Author.Id, updateTask);

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

		private EmbedBuilder CreateGiveawayEmbed(EventContext e, string text)
		{
			return new EmbedBuilder()
			{
				Author = new EmbedAuthor()
				{
					Name = e.Author.Username + " is giving away",
					IconUrl = e.Author.GetAvatarUrl()
				},
				ThumbnailUrl = "https://i.imgur.com/rIDHtwN.png",
				Description = text,
				Color = new Color(253, 216, 136),
			};
		}

		[Command(Name = "guildinfo")]
		public async Task GuildInfoAsync(EventContext e)
		{
			IDiscordGuildUser owner = await e.Guild.GetOwnerAsync();

			//var emojiNames = e.Guild.R.Select(x => x.ToString());
			string emojiOutput = "none (yet!)";

            //if (emojiNames.Count() > 0)
            //{	
            //	emojiOutput = string.Join(",", emojiNames);
            //}

            using (var context = new MikiContext())
            {
                string prefix = await e.commandHandler.GetDefaultPrefixValueAsync(context, e.Guild.Id);

                var roles = await e.Guild.GetRolesAsync();
                var channels = await e.Guild.GetChannelsAsync();

                await new EmbedBuilder()
                {
                    Author = new EmbedAuthor()
                    {
                        Name = e.Guild.Name,
                        IconUrl = e.Guild.IconUrl,
                        Url = e.Guild.IconUrl
                    },
                }.AddInlineField("👑 " + e.Locale.GetString("miki_module_general_guildinfo_owned_by"), $"{owner.Username}#{owner.Discriminator}")
                .AddInlineField("👉 " + e.Locale.GetString("miki_label_prefix"), prefix)
                .AddInlineField("📺 " + e.Locale.GetString("miki_module_general_guildinfo_channels"), channels.Count(x => x.Type == ChannelType.GUILDTEXT).ToFormattedString())
                .AddInlineField("🔊 " + e.Locale.GetString("miki_module_general_guildinfo_voicechannels"), channels.Count(x => x.Type == ChannelType.GUILDVOICE).ToFormattedString())
                .AddInlineField("🙎 " + e.Locale.GetString("miki_module_general_guildinfo_users"), roles.Count().ToFormattedString())
                .AddInlineField("#⃣ " + e.Locale.GetString("miki_module_general_guildinfo_roles_count"), roles.Count().ToFormattedString())
                .AddField("📜 " + e.Locale.GetString("miki_module_general_guildinfo_roles"),
                    string.Join(",", roles.Select(x => $"`{x.Name}`")))
                .AddField("😃 " + e.Locale.GetString("term_emoji"), emojiOutput)
                .ToEmbed().QueueToChannelAsync(e.Channel);
            }
		}

		[Command(Name = "help")]
		public async Task HelpAsync(EventContext e)
		{
			if (e.Arguments.Take(out string arg))
			{
				CommandEvent ev = e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Commands
					.FirstOrDefault(x => x.Name.ToLower() == arg.ToString().ToLower());

				if (ev == null)
				{
                    using (var context = new MikiContext())
                    {
                        EmbedBuilder helpListEmbed = new EmbedBuilder();
                        helpListEmbed.Title = e.Locale.GetString("miki_module_help_error_null_header");

                        helpListEmbed.Description = e.Locale.GetString("miki_module_help_error_null_message", 
                            await e.EventSystem.GetCommandHandler<SimpleCommandHandler>()
                                .GetDefaultPrefixValueAsync(context, e.Guild.Id));

                        helpListEmbed.Color = new Color(0.6f, 0.6f, 1.0f);

                        API.StringComparison.StringComparer comparer = new API.StringComparison.StringComparer(e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Commands.Select(x => x.Name));
                        API.StringComparison.StringComparison best = comparer.GetBest(arg);

                        helpListEmbed.AddField(e.Locale.GetString("miki_module_help_didyoumean"), best.text);

                        await helpListEmbed.ToEmbed()
                            .QueueToChannelAsync(e.Channel);
                    }
				}
				else
				{
					if (await e.EventSystem.GetCommandHandler<SimpleCommandHandler>().GetUserAccessibility(e) < ev.Accessibility)
					{
						return;
					}

					EmbedBuilder explainedHelpEmbed = new EmbedBuilder()
						.SetTitle(ev.Name.ToUpper());

					if (ev.Aliases.Length > 0)
					{
						explainedHelpEmbed.AddInlineField(
							e.Locale.GetString("miki_module_general_help_aliases"),
							string.Join(", ", ev.Aliases));
					}

					explainedHelpEmbed.AddField
					(
						e.Locale.GetString("miki_module_general_help_description"),
						e.Locale.HasString("miki_command_description_" + ev.Name.ToLower())
							? e.Locale.GetString("miki_command_description_" + ev.Name.ToLower())
							: e.Locale.GetString("miki_placeholder_null"));

					explainedHelpEmbed.AddField(
						e.Locale.GetString("miki_module_general_help_usage"),
						e.Locale.HasString("miki_command_usage_" + ev.Name.ToLower())
							? e.Locale.GetString("miki_command_usage_" + ev.Name.ToLower()) : e.Locale.GetString("miki_placeholder_null"));

                    await explainedHelpEmbed.ToEmbed().QueueToChannelAsync(e.Channel);
				}
				return;
			}

            await new EmbedBuilder()
			{
				Description = e.Locale.GetString("miki_module_general_help_dm"),
				Color = new Color(0.6f, 0.6f, 1.0f)
			}.ToEmbed().QueueToChannelAsync(e.Channel);

			EmbedBuilder embedBuilder = new EmbedBuilder();

			foreach (Module m in e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Modules.OrderBy(x => x.Name))
			{
				List<CommandEvent> events = m.Events
					.Where(x => e.EventSystem.GetCommandHandler<SimpleCommandHandler>().GetUserAccessibility(e).Result >= x.Accessibility).ToList();

				if (events.Count > 0)
				{
					embedBuilder.AddField(m.Name.ToUpper(), string.Join(", ", events.Select(x => "`" + x.Name + "`")));
				}
			}

            await embedBuilder.ToEmbed().QueueToChannelAsync(await e.Author.GetDMChannelAsync(), "Join our support server: https://discord.gg/39Xpj7K");
		}

		[Command(Name = "donate", Aliases = new string[] { "patreon" })]
		public async Task DonateAsync(EventContext e)
		{
            await new EmbedBuilder()
			{
				Title = "Hi everyone!",
				Description = e.Locale.GetString("miki_module_general_info_donate_string"),
				Color = new Color(0.8f, 0.4f, 0.4f),
				ThumbnailUrl = "https://trello-attachments.s3.amazonaws.com/57acf354029527926a15e83d/598763ed8a7735cb8b52cd72/1d168f6025e40b9c6b53c3d4b8e07ccf/xdmemes.png",
			}.AddField("Links", "https://www.patreon.com/mikibot - if you want to donate every month and get cool rewards!\nhttps://ko-fi.com/velddy - one time donations please include your discord name#identifiers so i can contact you!", true)
			.AddField("Don't have money?", "You can always support us in different ways too! Please participate in our [idea](https://suggestions.miki.ai/) discussions so we can get a better grasp of what you guys would like to see next! Or vote for Miki on [Discordbots.org](https://discordbots.org/bot/160105994217586689)", true)
			.ToEmbed().QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "info", Aliases = new string[] { "about" })]
		public async Task InfoAsync(EventContext e)
		{
			EmbedBuilder embed = new EmbedBuilder();

			embed.SetAuthor("Miki " + Global.Version, "", "");
			embed.Color = new Color(0.6f, 0.6f, 1.0f);

			embed.AddField(e.Locale.GetString("miki_module_general_info_made_by_header"),
				e.Locale.GetString("miki_module_general_info_made_by_description") + " Fuzen, IA, Rappy, Tal, Vallode, GrammarJew");

			embed.AddField(e.Locale.GetString("miki_module_general_info_links"),
				$"`{e.Locale.GetString("miki_module_general_info_docs").PadRight(15)}:` [documentation](https://www.github.com/velddev/miki/wiki)\n" +
				$"`{"donate".PadRight(15)}:` [patreon](https://www.patreon.com/mikibot) | [ko-fi](https://ko-fi.com/velddy)\n" +
				$"`{e.Locale.GetString("miki_module_general_info_twitter").PadRight(15)}:` [veld](https://www.twitter.com/velddev) | [miki](https://www.twitter.com/miki_discord)\n" +
				$"`{e.Locale.GetString("miki_module_general_info_reddit").PadRight(15)}:` [/r/mikibot](https://www.reddit.com/r/mikibot) \n" +
				$"`{e.Locale.GetString("miki_module_general_info_server").PadRight(15)}:` [discord](https://discord.gg/39Xpj7K)\n" +
				$"`{e.Locale.GetString("miki_module_general_info_website").PadRight(15)}:` [link](https://miki.ai) [suggestions](https://suggestions.miki.ai/)");

            await embed.ToEmbed().QueueToChannelAsync(e.Channel);

			await Task.Yield();
		}

		[Command(Name = "invite")]
		public async Task InviteAsync(EventContext e)
		{
			e.Channel.QueueMessage(e.Locale.GetString("miki_module_general_invite_message"));
			(await e.Author.GetDMChannelAsync()).QueueMessage(e.Locale.GetString("miki_module_general_invite_dm")
				+ "\nhttps://discordapp.com/oauth2/authorize?&client_id=160185389313818624&scope=bot&permissions=355593334");
		}

		[Command(Name = "ping", Aliases = new string[] { "lag" })]
		public async Task PingAsync(EventContext e)
		{
            (await e.CreateEmbedBuilder()
				  .WithTitle(new RawResource("Ping"))
				  .WithDescription("ping_placeholder")
				  .Build()
				  .QueueToChannelAsync(e.Channel))
				  .ThenWait(200)
				  .Then(async x =>
				  {
					  float ping = (float)(x.Timestamp - e.message.Timestamp).TotalMilliseconds;
					  DiscordEmbed embed = new EmbedBuilder()
						  .SetTitle("Pong - " + Environment.MachineName)
						  .SetColor(Color.Lerp(new Color(0.0f, 1.0f, 0.0f), new Color(1.0f, 0.0f, 0.0f), Math.Min(ping / 1000, 1f)))
						  .AddInlineField("Miki", ((int)ping).ToFormattedString() + "ms").ToEmbed();

                      await embed.EditAsync(x);
				  });
		}

		[Command(Name = "prefix")]
		public async Task PrefixHelpAsync(EventContext e)
		{
            using (var context = new MikiContext())
            {
                await e.CreateEmbedBuilder()
                    .WithTitle("miki_module_general_prefix_help_header")
                    .WithDescription("prefix_info", await e.commandHandler.GetDefaultPrefixValueAsync(context, e.Guild.Id))
                    .Build().QueueToChannelAsync(e.Channel);
            }
		}

		[Command(Name = "stats")]
		public async Task StatsAsync(EventContext e)
		{
			await Task.Yield();

			var cache = MikiApp.Instance.Discord.CacheClient;

            await new EmbedBuilder()
			{
				Title = "⚙️ Miki stats",
				Description = e.Locale.GetString("stats_description"),
				Color = new Color(0.3f, 0.8f, 1),
			}.AddField($"🖥️ {e.Locale.GetString("discord_servers")}", (await cache.HashLengthAsync(CacheUtils.GuildsCacheKey)).ToFormattedString())
			 .AddField("More info", "https://p.datadoghq.com/sb/01d4dd097-08d1558da4")
			 .ToEmbed().QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "urban")]
		public async Task UrbanAsync(EventContext e)
		{
            if (!e.Arguments.Pack.CanTake)
            {
                return;
            }

            var api = (UrbanDictionaryAPI)e.Services.GetService(typeof(UrbanDictionaryAPI));

            var query = e.Arguments.Pack.TakeAll();
            var searchResult = await api.SearchTermAsync(query);

            if(searchResult == null)
            {
                // TODO (Veld): Something went wrong/No results found.
                return;
            }

            UrbanDictionaryEntry entry = searchResult.Entries
                .FirstOrDefault();

			if (entry != null)
			{
                string desc = Regex.Replace(entry.Definition, "\\[(.*?)\\]", 
                    (x) => $"[{x.Groups[1].Value}]({api.GetUserDefinitionURL(x.Groups[1].Value)})"
                    );

                string example = Regex.Replace(entry.Example, "\\[(.*?)\\]",
                    (x) => $"[{x.Groups[1].Value}]({api.GetUserDefinitionURL(x.Groups[1].Value)})"
                    );

                await new EmbedBuilder()
				{
					Author = new EmbedAuthor()
					{
						Name = "📚 " + entry.Term,
						Url = "http://www.urbandictionary.com/define.php?term=" + query,
					},
					Description = e.Locale.GetString("miki_module_general_urban_author", entry.Author)
				}.AddField(e.Locale.GetString("miki_module_general_urban_definition"), desc, true)
				 .AddField(e.Locale.GetString("miki_module_general_urban_example"), example, true)
				 .AddField(e.Locale.GetString("miki_module_general_urban_rating"), "👍 " + entry.ThumbsUp.ToFormattedString() + "  👎 " + entry.ThumbsDown.ToFormattedString(), true)
				 .ToEmbed().QueueToChannelAsync(e.Channel);
			}
			else
			{
                await e.ErrorEmbed(e.Locale.GetString("error_term_invalid"))
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "whois")]
		public async Task WhoIsAsync(EventContext e)
		{
			if (!e.Arguments.Take(out string arg))
			{
				throw new ArgumentNullException();
			}

			IDiscordGuildUser user = await DiscordExtensions.GetUserAsync(arg, e.Guild);

			//if (user == null)
			//{
			//	user = e.Author as IGuildUser;
			//}

			var embed = e.CreateEmbedBuilder();
			embed.WithTitle("whois_title", user.Username);
			embed.SetColor(0.5f, 0f, 1.0f);

			embed.SetImage(user.GetAvatarUrl());

			var roles = (await e.Guild.GetRolesAsync()).Where(x => user.RoleIds?.Contains(x.Id) ?? false && x.Color.Value != 0).OrderByDescending(x => x.Position);

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

            await embed.Build().QueueToChannelAsync(e.Channel);
		}
	}
}