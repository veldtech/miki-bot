using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Miki.API.UrbanDictionary;
using Miki.Languages;
using Miki.Models;
using NCalc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Miki.API;
using Miki.Dsl;
using Miki.Framework.Extension;
using Microsoft.EntityFrameworkCore;
using Miki.Framework.Languages;
using System.Text;
using Miki.Framework.Language;
using Miki.Configuration;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Cache;
using Miki.Helpers;

namespace Miki.Modules
{
	[Module("General")]
	internal class GeneralModule
	{
		[Configurable]
		public string UrbanKey { get; set; } = "";

		TaskScheduler<string> taskScheduler = new TaskScheduler<string>();

		public GeneralModule(Module m, Framework.Bot b)
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
			ArgObject arg = e.Arguments.FirstOrDefault();

			if(arg == null)
			{
				e.Channel.QueueMessageAsync(e.Author.GetAvatarUrl());
			}
			else
			{
				if (arg.Argument == "-s")
				{
					e.Channel.QueueMessageAsync(e.Guild.IconUrl);
					return;
				}

				IDiscordGuildUser user = await arg.GetUserAsync(e.Guild);
				
				if(user != null)
				{
					e.Channel.QueueMessageAsync(user.GetAvatarUrl());
				}
			}
		}

		[Command(Name = "calc", Aliases = new string[] { "calculate" })]
		public async Task CalculateAsync(EventContext e)
		{
			try
			{
				Expression expression = new Expression(e.Arguments.ToString());

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

				e.Channel.QueueMessageAsync(output.ToString());
			}
			catch (Exception ex)
			{
				e.Channel.QueueMessageAsync(e.Locale.GetString("miki_module_general_calc_error") + "\n```" + ex.Message + "```");
			}
		}

		[Command(Name = "changelog")]
		public async Task ChangelogAsync(EventContext e)
		{
			await Task.Yield();
			new EmbedBuilder()
			{
				Title = "Changelog",
				Description = "Check out my changelog blog [here](https://blog.miki.ai/)!"
			}.ToEmbed().QueueToChannel(e.Channel);
		}

		[Command(Name = "giveaway")]
		public async Task GiveawayAsync(EventContext e)
		{
			//Emoji emoji = new Emoji("🎁");

			var arg = e.Arguments.FirstOrDefault();
			string giveAwayText = arg?.Argument ?? "";
			arg = arg?.Next();

			while (!(arg?.Argument ?? "-").StartsWith("-"))
			{
				giveAwayText += " " + arg.Argument;
				arg = arg?.Next();
			}

			var mml = new MMLParser(arg?.TakeUntilEnd()?.Argument ?? "").Parse();

			bool isUnique = mml.Get("unique", false);
			int amount = mml.Get("amount", 1);
			TimeSpan timeLeft = mml.Get("time", "1h").GetTimeFromString();

			if(amount > 10)
			{
				e.ErrorEmbed("We can only allow up to 10 picks per giveaway")
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			giveAwayText = giveAwayText + ((amount > 1) ? " x " + amount : "");

			List<IDiscordUser> winners = new List<IDiscordUser>();

			IDiscordMessage msg = await CreateGiveawayEmbed(e, giveAwayText)
			.AddField("Time", (DateTime.Now + timeLeft).ToShortTimeString(), true)
			.AddField("React to participate", "good luck", true)
			.ToEmbed().SendToChannel(e.Channel);

			//await (msg as IUserMessage).AddReactionAsync(emoji);

			int updateTask = -1;

			int task = taskScheduler.AddTask(e.Author.Id, async (desc) =>
			{
				//msg = await e.Channel.GetMessageAsync(msg.Id);

				if (msg != null)
				{
					//await msg.RemoveReactionAsync(emoji, await e.Guild.GetCurrentUserAsync());

					List<IDiscordUser> reactions = new List<IDiscordUser>();

					int reactionsGained = 0;

					do
					{
						//reactions.AddRange(await (msg as  IUserMessage).GetReactionUsersAsync(emoji, 100, reactions.LastOrDefault()?.Id ?? null));
						reactionsGained += 100;
					} while (reactions.Count == reactionsGained);

					// Select random winners
					for (int i = 0; i < amount; i++)
					{
						if (reactions.Count == 0)
							break;

						int index = MikiRandom.Next(reactions.Count);

						winners.Add(reactions[index]);

						if (isUnique)
							reactions.RemoveAt(index);
					}

					if (updateTask != -1)
						taskScheduler.CancelReminder(e.Author.Id, updateTask);

					string winnerText = string.Join("\n", winners.Select(x => x.Username + "#" + x.Discriminator).ToArray());
					if (string.IsNullOrEmpty(winnerText))
						winnerText = "nobody!";

					await msg.EditAsync(new EditMessageArgs
					{
						embed = CreateGiveawayEmbed(e, giveAwayText)
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

			//var emojiNames = e.Guild.Emotes.Select(x => x.ToString());
			string emojiOutput = "none (yet!)";

			//if(emojiNames.Count() > 0)
			//{
			//	emojiOutput = string.Join(",", emojiNames);
			//}

			string prefix = await e.commandHandler.GetDefaultPrefixValueAsync(e.Guild.Id);

			var roles = await e.Guild.GetRolesAsync();
			var channels = await e.Guild.GetChannelsAsync();

			new EmbedBuilder()
			{
				Author = new EmbedAuthor()
				{
					Name = e.Guild.Name,
					IconUrl = e.Guild.IconUrl,
					Url = e.Guild.IconUrl
				},
			}.AddInlineField("👑 " + e.Locale.GetString("miki_module_general_guildinfo_owned_by"), $"{owner.Username}#{owner.Discriminator}")
			.AddInlineField("👉 " +  e.Locale.GetString("miki_label_prefix"), prefix)
			.AddInlineField("📺 " +  e.Locale.GetString("miki_module_general_guildinfo_channels"), channels.Count(x => x.Type == ChannelType.GUILDTEXT).ToString())
			.AddInlineField("🔊 " +  e.Locale.GetString("miki_module_general_guildinfo_voicechannels"), channels.Count(x => x.Type == ChannelType.GUILDVOICE).ToString())
			.AddInlineField("🙎 " +  e.Locale.GetString("miki_module_general_guildinfo_users"), roles.Count().ToString())
			.AddInlineField("#⃣ " +  e.Locale.GetString("miki_module_general_guildinfo_roles_count"), roles.Count().ToString())
			.AddField("📜 " +  e.Locale.GetString("miki_module_general_guildinfo_roles"), 
				string.Join(",", roles.Select(x => $"`{x.Name}`")))
			.AddField("😃 " + e.Locale.GetString("term_emoji"), emojiOutput)
			.ToEmbed().QueueToChannel(e.Channel);
		}

		[Command(Name = "help")]
		public async Task HelpAsync(EventContext e)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg != null)
			{
				CommandEvent ev = e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Commands.FirstOrDefault(x => x.Name.ToLower() == arg.Argument.ToString().ToLower());

				if (ev == null)
				{
					EmbedBuilder helpListEmbed = Utils.Embed;
					helpListEmbed.Title = e.Locale.GetString("miki_module_help_error_null_header");

					helpListEmbed.Description = e.Locale.GetString("miki_module_help_error_null_message", await e.EventSystem.GetCommandHandler<SimpleCommandHandler>().GetDefaultPrefixValueAsync(e.Guild.Id));
					helpListEmbed.Color = new Color(0.6f, 0.6f, 1.0f);

					API.StringComparison.StringComparer comparer = new API.StringComparison.StringComparer(e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Commands.Select(x => x.Name));
					API.StringComparison.StringComparison best = comparer.GetBest(arg.Argument);

					helpListEmbed.AddField(e.Locale.GetString("miki_module_help_didyoumean"), best.text);

					helpListEmbed.ToEmbed()
						.QueueToChannel(e.Channel);
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

					explainedHelpEmbed.ToEmbed().QueueToChannel(e.Channel);
				}
				return;
			}

			new EmbedBuilder()
			{
				Description = e.Locale.GetString("miki_module_general_help_dm"),
				Color = new Color(0.6f, 0.6f, 1.0f)
			}.ToEmbed().QueueToChannel(e.Channel);


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

			embedBuilder.ToEmbed().QueueToChannel(await e.Author.GetDMChannelAsync());
		}

		[Command(Name = "donate", Aliases = new string[] { "patreon" })]
		public async Task DonateAsync(EventContext e)
		{
			new EmbedBuilder()
			{
				Title = "Hi everyone!",
				Description = e.Locale.GetString("miki_module_general_info_donate_string"),
				Color = new Color(0.8f, 0.4f, 0.4f),
				ThumbnailUrl = "https://trello-attachments.s3.amazonaws.com/57acf354029527926a15e83d/598763ed8a7735cb8b52cd72/1d168f6025e40b9c6b53c3d4b8e07ccf/xdmemes.png",
			}.AddField("Links", "https://www.patreon.com/mikibot - if you want to donate every month and get cool rewards!\nhttps://ko-fi.com/velddy - one time donations please include your discord name#identifiers so i can contact you!", true)
			.AddField("Don't have money?", "You can always support us in different ways too! Please participate in our [idea](https://suggestions.miki.ai/) discussions so we can get a better grasp of what you guys would like to see next! Or vote for Miki on [Discordbots.org](https://discordbots.org/bot/160105994217586689)", true)
			.ToEmbed().QueueToChannel(e.Channel);
		}

		[Command(Name = "info", Aliases = new string[] { "about" })]
		public async Task InfoAsync(EventContext e)
		{
			EmbedBuilder embed = new EmbedBuilder();

			embed.SetAuthor("Miki " + Framework.Bot.Instance.Information.Version, "" ,"");
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

			embed.ToEmbed().QueueToChannel(e.Channel);

			await Task.Yield();
		}

		[Command(Name = "invite")]
		public async Task InviteAsync(EventContext e)
		{
			e.Channel.QueueMessageAsync(e.Locale.GetString("miki_module_general_invite_message"));
			(await e.Author.GetDMChannelAsync()).QueueMessageAsync(e.Locale.GetString("miki_module_general_invite_dm")
				+ "\nhttps://discordapp.com/oauth2/authorize?&client_id=160185389313818624&scope=bot&permissions=355593334");
		}

		[Command(Name = "ping", Aliases = new string[] { "lag" })]
		public async Task PingAsync(EventContext e)
		{
			IDiscordMessage message = await e.CreateEmbedBuilder()
				.WithTitle(new RawResource("Ping"))
				.WithDescription("ping_placeholder")
				.Build()
				.SendToChannel(e.Channel);

			await Task.Delay(100);

			if (message != null)
			{
				float ping = (float)(message.Timestamp - e.message.Timestamp).TotalMilliseconds;

				DiscordEmbed embed = new EmbedBuilder()
					.SetTitle("Pong - " + Environment.MachineName)
					.SetColor(Color.Lerp(new Color(0.0f, 1.0f, 0.0f), new Color(1.0f, 0.0f, 0.0f), Math.Min(ping / 1000, 1f)))
					.AddInlineField("Miki", ping + "ms").ToEmbed();

				await message.EditAsync(new EditMessageArgs
				{
					content = "",
					embed = embed
				});
			}
		}

		[Command(Name = "prefix")]
		public async Task PrefixHelpAsync(EventContext e)
		{
			e.CreateEmbedBuilder()
				.WithTitle("miki_module_general_prefix_help_header")
				.WithDescription("prefix_info", await e.commandHandler.GetDefaultPrefixValueAsync(e.Guild.Id))
				.Build().QueueToChannel(e.Channel);
		}

		[Command(Name = "stats")]
		public async Task StatsAsync(EventContext e)
		{
			await Task.Yield();

			TimeSpan timeSinceStart = DateTime.Now.Subtract(Program.timeSinceStartup);

			var cache = await Framework.Bot.Instance.CachePool.GetAsync() as IExtendedCacheClient;

			new EmbedBuilder()
			{
				Title = "⚙️ Miki stats",
				Description = e.Locale.GetString("stats_description"),
				Color = new Color(0.3f, 0.8f, 1),
			}.AddField($"🖥️ {e.Locale.GetString("discord_servers")}", await cache.HashLengthAsync(CacheUtils.GuildsCacheKey()))
			 .AddField("⏰ Uptime", timeSinceStart.ToTimeString(e.Locale))
			 .AddField("More info", "https://p.datadoghq.com/sb/01d4dd097-08d1558da4")
			 .ToEmbed().QueueToChannel(e.Channel);
		}

		[Command(Name = "urban")]
		public async Task UrbanAsync(EventContext e)
		{
			if (string.IsNullOrEmpty(e.Arguments.ToString()))
				return;

			UrbanDictionaryApi api = new UrbanDictionaryApi(UrbanKey);
			UrbanDictionaryEntry entry = await api.GetEntryAsync(e.Arguments.ToString());

			if (entry != null)
			{
				new EmbedBuilder()
				{
					Author = new EmbedAuthor()
					{
						Name = entry.Term,
						IconUrl = "http://cdn9.staztic.com/app/a/291/291148/urban-dictionary-647813-l-140x140.png",
						Url = "http://www.urbandictionary.com/define.php?term=" + e.Arguments.ToString(),
					},
					Description = e.Locale.GetString("miki_module_general_urban_author", entry.Author)
				}.AddField(e.Locale.GetString("miki_module_general_urban_definition"), entry.Definition, true)
				.AddField(e.Locale.GetString("miki_module_general_urban_example"), entry.Example, true)
				.AddField(e.Locale.GetString("miki_module_general_urban_rating"), 
				"👍 " + entry.ThumbsUp + "  👎 " + entry.ThumbsDown, true)
				.ToEmbed().QueueToChannel(e.Channel);
			}
			else
			{
				e.ErrorEmbed(e.Locale.GetString("error_term_invalid"))
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "whois")]
		public async Task WhoIsAsync(EventContext e)
		{
			ArgObject arg = e.Arguments.Join();

			if (arg == null)
			{
				throw new ArgumentNullException();
			}

			IDiscordGuildUser user = await arg.GetUserAsync(e.Guild);

			//if (user == null)
			//{
			//	user = e.Author as IGuildUser;
			//}

			var embed = e.CreateEmbedBuilder();
			embed.WithTitle("whois_title", user.Username);
			embed.EmbedBuilder.SetColor(0.5f, 0f, 1.0f);

			embed.EmbedBuilder.ImageUrl = user.GetAvatarUrl();

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

			embed.Build().QueueToChannel(e.Channel);
		}
	}
}
