using Discord;
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

namespace Miki.Modules
{
	[Module("General")]
	internal class GeneralModule
	{
		TaskScheduler<string> taskScheduler = new TaskScheduler<string>();

		public GeneralModule(Module m)
		{
			EventSystem.Instance.AddCommandDoneEvent(x =>
			{
				x.Name = "--count-commands";
				x.processEvent = async (msg, e, success, t) =>
				{
					if (success)
					{
						using (var context = new MikiContext())
						{
							User user = await User.GetAsync(context, msg.Author);
							CommandUsage u = await CommandUsage.GetAsync(context, msg.Author.Id.ToDbLong(), e.Name);

							u.Amount++;
							user.Total_Commands++;

							await CommandUsage.UpdateCacheAsync(user.Id, e.Name, u);
							await context.SaveChangesAsync();
						}
					}
				};
			});
		}

		[Command(Name = "avatar")]
		public async Task AvatarAsync(EventContext e)
		{
			if (e.message.MentionedUserIds.Count > 0)
			{
				e.Channel.QueueMessageAsync(string.Join(".", (await e.Guild.GetUserAsync(e.message.MentionedUserIds.First())).GetAvatarUrl()));
			}
			else
			{
				e.Channel.QueueMessageAsync(string.Join(".", e.Author.GetAvatarUrl()));
			}
		}

		[Command(Name = "avatar", On = "-s")]
		public async Task ServerAvatarAsync(EventContext e)
		{
			e.Channel.QueueMessageAsync(string.Join(".", e.Guild.IconUrl));
			await Task.Yield();
		}

		[Command(Name = "calc", Aliases = new string[] { "calculate" })]
		public async Task CalculateAsync(EventContext e)
		{
			Locale locale = new Locale(e.Channel.Id);

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
				e.Channel.QueueMessageAsync(locale.GetString("miki_module_general_calc_error") + "\n```" + ex.Message + "```");
			}
		}

		[Command(Name = "changelog")]
		public async Task ChangelogAsync(EventContext e)
		{
			await Task.Yield();
			new EmbedBuilder()
			{
				Title = "Changelog",
				Description = "Check out my changelog blog [here](https://medium.com/@velddev)"
			}.Build().QueueToChannel(e.Channel);
		}

		[Command(Name = "giveaway")]
		public async Task GiveawayAsync(EventContext e)
		{
			Emoji emoji = new Emoji("🎁");

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

			giveAwayText = giveAwayText + ((amount > 1) ? " x " + amount : "");

			List<IUser> winners = new List<IUser>();

			IMessage msg = await CreateGiveawayEmbed(e, giveAwayText)
			.AddField("Time Remaining", timeLeft.ToTimeString(e.Channel.GetLocale(), true), true)
			.AddField("React to participate", "good luck", true)
			.Build().SendToChannel(e.Channel);

			await (msg as IUserMessage).AddReactionAsync(emoji);

			int updateTask = -1;

			int task = taskScheduler.AddTask(e.Author.Id, async (desc) =>
			{
				await (msg as IUserMessage).RemoveReactionAsync(emoji, await e.Guild.GetCurrentUserAsync());

				List<IUser> reactions = new List<IUser>();

				int reactionsGained = 0;

				do
				{
					reactions.AddRange(await (msg as IUserMessage).GetReactionUsersAsync(emoji, 100, reactions.LastOrDefault()?.Id ?? null));
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

				await (msg as IUserMessage).ModifyAsync(x =>
				{
					x.Embed = CreateGiveawayEmbed(e, giveAwayText)
						.AddField("Winners", winnerText)
						.Build();
				});

			}, "description var", timeLeft);

			updateTask = taskScheduler.AddTask(e.Author.Id, async (_) =>
			{
				var taskInstance = taskScheduler.GetInstance(e.Author.Id, task);

				if (taskInstance != null)
				{
					await (msg as IUserMessage).ModifyAsync(x =>
					{
						x.Embed = CreateGiveawayEmbed(e, giveAwayText)
							.AddField("Time Remaining", timeLeft.ToTimeString(e.Channel.GetLocale(), true), true)
							.AddField("React to participate", "good luck", true)
							.Build();
					});
				}
			}, "", new TimeSpan(0, 5, 0), true);
		}

		private EmbedBuilder CreateGiveawayEmbed(EventContext e, string text)
		{
			return new EmbedBuilder()
			{
				Author = new EmbedAuthorBuilder()
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
			Locale l = new Locale(e.Channel.Id);

			IGuildUser owner = await e.Guild.GetOwnerAsync();

			new EmbedBuilder()
			{
				Author = new EmbedAuthorBuilder()
				{
					Name = e.Guild.Name,
					IconUrl = e.Guild.IconUrl,
					Url = e.Guild.IconUrl
				},
			}.AddField("👑" + l.GetString("miki_module_general_guildinfo_owned_by"), owner.Username + "#" + owner.Discriminator, true)
			.AddField("👉" + l.GetString("miki_label_prefix"), await PrefixInstance.Default.GetForGuildAsync(e.Guild.Id), true)
			.AddField("📺" + l.GetString("miki_module_general_guildinfo_channels"), (await e.Guild.GetChannelsAsync()).Count.ToString(), true)
			.AddField("🔊" + l.GetString("miki_module_general_guildinfo_voicechannels"), (await e.Guild.GetVoiceChannelsAsync()).Count.ToString(), true)
			.AddField("🙎" + l.GetString("miki_module_general_guildinfo_users"), (await e.Guild.GetUsersAsync()).Count.ToString(), true)
			.AddField("🤖" + l.GetString("term_shard"), Bot.Instance.Client.GetShardFor(e.Guild).ShardId, true)
			.AddField("#⃣" + l.GetString("miki_module_general_guildinfo_roles_count"), e.Guild.Roles.Count.ToString(), true)
			.AddField("📜" + l.GetString("miki_module_general_guildinfo_roles"), string.Join(",", e.Guild.Roles.Select(x => $"`{x.Name}`")))
			.Build().QueueToChannel(e.Channel);
		}

		[Command(Name = "help")]
		public async Task HelpAsync(EventContext e)
		{
			Locale locale = new Locale(e.Channel.Id);

			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg != null)
			{
				CommandEvent ev = EventSystem.Instance.CommandHandler.GetCommandEvent(arg.Argument);

				if (ev == null)
				{
					EmbedBuilder helpListEmbed = Utils.Embed;
					helpListEmbed.Title = locale.GetString("miki_module_help_error_null_header");
					helpListEmbed.Description = locale.GetString("miki_module_help_error_null_message", await EventSystem.Instance.GetPrefixInstance(">").GetForGuildAsync(e.Guild.Id));
					helpListEmbed.Color = new Color(0.6f, 0.6f, 1.0f);

					API.StringComparison.StringComparer comparer = new API.StringComparison.StringComparer(e.commandHandler.GetAllEventNames());
					API.StringComparison.StringComparison best = comparer.GetBest(arg.Argument);

					helpListEmbed.AddField(locale.GetString("miki_module_help_didyoumean"), best.text);

					helpListEmbed.Build()
						.QueueToChannel(e.Channel);
				}
				else
				{
					if (EventSystem.Instance.CommandHandler.GetUserAccessibility(e.message) < ev.Accessibility)
					{
						return;
					}

					EmbedBuilder explainedHelpEmbed = new EmbedBuilder()
					{
						Title = ev.Name.ToUpper()
					};

					if (ev.Aliases.Length > 0)
					{
						explainedHelpEmbed.AddInlineField(
							locale.GetString("miki_module_general_help_aliases"),
							string.Join(", ", ev.Aliases));
					}

					explainedHelpEmbed.AddField(
						locale.GetString("miki_module_general_help_description"),
						(locale.HasString("miki_command_description_" + ev.Name.ToLower())) ? locale.GetString("miki_command_description_" + ev.Name.ToLower()) : locale.GetString("miki_placeholder_null"));

					explainedHelpEmbed.AddField(
						locale.GetString("miki_module_general_help_usage"),
						(locale.HasString("miki_command_usage_" + ev.Name.ToLower())) ? locale.GetString("miki_command_usage_" + ev.Name.ToLower()) : locale.GetString("miki_placeholder_null"));

					explainedHelpEmbed.Build().QueueToChannel(e.Channel);
				}
				return;
			}

			new EmbedBuilder()
			{
				Description = locale.GetString("miki_module_general_help_dm"),
				Color = new Color(0.6f, 0.6f, 1.0f)
			}.Build().QueueToChannel(e.Channel);

			(await EventSystem.Instance.ListCommandsInEmbedAsync(e.message))
				.QueueToUser(e.Author);
		}

		[Command(Name = "donate", Aliases = new string[] { "patreon" })]
		public async Task DonateAsync(EventContext e)
		{
			Locale locale = new Locale(e.Channel.Id);
			new EmbedBuilder()
			{
				Title = "Hi everyone!",
				Description = e.GetResource("miki_module_general_info_donate_string"),
				Color = new Color(0.8f, 0.4f, 0.4f),
				ThumbnailUrl = "https://trello-attachments.s3.amazonaws.com/57acf354029527926a15e83d/598763ed8a7735cb8b52cd72/1d168f6025e40b9c6b53c3d4b8e07ccf/xdmemes.png",
			}.AddField("Links", "https://www.patreon.com/mikibot - if you want to donate every month and get cool rewards!\nhttps://ko-fi.com/velddy - one time donations please include your discord name#identifiers so i can contact you!", true)
			.AddField("Don't have money?", "You can always support us in different ways too! Please participate in our [Trello](https://trello.com/b/SdjIVMtx/miki) discussion so we can get a better grasp of what you guys would like to see next! Or vote for Miki on [Discordbots.org](https://discordbots.org/bot/160105994217586689)", true)
			.AddField("Don't **Want** to send me money?", "And still want to support me? I do have an [Amazon wishlist](https://www.amazon.de/hz/wishlist/ls/14YC7IAHJBU4O) for all kinds of hobbies and things to teach myself. You could also send something from here")
			.Build().QueueToChannel(e.Channel);
		}

		[Command(Name = "info", Aliases = new string[] { "about" })]
		public async Task InfoAsync(EventContext e)
		{
			EmbedBuilder embed = new EmbedBuilder();
			Locale locale = new Locale(e.Channel.Id);

			embed.SetAuthor("Miki " + Bot.Instance.Information.Version, "" ,"");
			embed.Color = new Color(0.6f, 0.6f, 1.0f);

			embed.AddField(locale.GetString("miki_module_general_info_made_by_header"),
				locale.GetString("miki_module_general_info_made_by_description") + " Drummss, Fuzen, IA, Luke, Milk, n0t, Phanrazak, Rappy, Tal, Vallode, GrammarJew");


			embed.AddField(e.GetResource("miki_module_general_info_links"),
				$"`{locale.GetString("miki_module_general_info_docs").PadRight(15)}:` [documentation](https://www.github.com/velddev/miki/wiki)\n" +
				$"`{"donate".PadRight(15)}:` [patreon](https://www.patreon.com/mikibot) | [ko-fi](https://ko-fi.com/velddy)\n" +
				$"`{locale.GetString("miki_module_general_info_twitter").PadRight(15)}:` [veld](https://www.twitter.com/velddev) | [miki](https://www.twitter.com/miki_discord)\n" +
				$"`{locale.GetString("miki_module_general_info_reddit").PadRight(15)}:` [/r/mikibot](https://www.reddit.com/r/mikibot) \n" +
				$"`{locale.GetString("miki_module_general_info_server").PadRight(15)}:` [discord](https://discord.gg/55sAjsW)\n" +
				$"`{locale.GetString("miki_module_general_info_website").PadRight(15)}:` [link](https://miki.ai)");

			embed.Build().QueueToChannel(e.Channel);

			await Task.Yield();
		}

		[Command(Name = "invite")]
		public async Task InviteAsync(EventContext e)
		{
			Locale locale = new Locale(e.Channel.Id);

			e.Channel.QueueMessageAsync(locale.GetString("miki_module_general_invite_message"));
			(await e.Author.GetOrCreateDMChannelAsync()).QueueMessageAsync(locale.GetString("miki_module_general_invite_dm")
				+ "\nhttps://discordapp.com/oauth2/authorize?&client_id=160185389313818624&scope=bot&permissions=355593334");
		}

		[Command(Name = "ping", Aliases = new string[] { "lag" })]
		public async Task PingAsync(EventContext e)
		{
			IUserMessage message = await new EmbedBuilder()
			{
				Title = "Ping",
				Description = e.GetResource("ping_placeholder")
			}.Build().SendToChannel(e.Channel);

			await Task.Delay(100);

			if (message != null)
			{
				double ping = (message.Timestamp - e.message.Timestamp).TotalMilliseconds;

				Embed embed = new EmbedBuilder()
				{
					Title = "Pong",
					Color = DiscordExtensions.Lerp(new Color(0, 1, 0), new Color(1, 0, 0), Mathm.Clamp((float)ping / 1000, 0, 1))
				}.AddInlineField("Miki", ping + "ms")
				.AddInlineField("Discord", Bot.Instance.Client.Latency + "ms").Build();
				
				await message.ModifyAsync(x => {
					x.Embed = embed;
				});
			}
		}

		[Command(Name = "prefix")]
		public async Task PrefixHelpAsync(EventContext e)
		{
			Locale locale = e.Channel.GetLocale();

			Utils.Embed.WithTitle(locale.GetString("miki_module_general_prefix_help_header"))
				.WithDescription(locale.GetString("miki_module_general_prefix_help", await PrefixInstance.Default.GetForGuildAsync(e.Guild.Id)))
				.Build().QueueToChannel(e.Channel);

			await Task.Yield();
		}

		[Command(Name = "stats")]
		public async Task StatsAsync(EventContext e)
		{
			await Task.Yield();

			TimeSpan timeSinceStart = DateTime.Now.Subtract(Program.timeSinceStartup);

			new EmbedBuilder()
			{
				Title = "⚙️ Miki stats",
				Description = e.GetResource("stats_description"),
				Color = new Color(0.3f, 0.8f, 1),
			}.AddField($"🖥️ {e.GetResource("discord_servers")}", Bot.Instance.Client.Guilds.Count.ToString())
			 .AddField("💬 " + e.GetResource("term_commands"), EventSystem.Instance.CommandsUsed)
			 .AddField("⏰ Uptime", timeSinceStart.ToTimeString(e.Channel.GetLocale()))
			 .AddField("More info", "https://p.datadoghq.com/sb/01d4dd097-08d1558da4")
			 .Build().QueueToChannel(e.Channel);

		}

		[Command(Name = "urban")]
		public async Task UrbanAsync(EventContext e)
		{
			if (string.IsNullOrEmpty(e.Arguments.ToString()))
				return;

			Locale locale = new Locale(e.Channel.Id);
			UrbanDictionaryApi api = new UrbanDictionaryApi(Global.Config.UrbanKey);
			UrbanDictionaryEntry entry = await api.GetEntryAsync(e.Arguments.ToString());

			if (entry != null)
			{
				new EmbedBuilder()
				{
					Author = new EmbedAuthorBuilder()
					{
						Name = entry.Term,
						IconUrl = "http://cdn9.staztic.com/app/a/291/291148/urban-dictionary-647813-l-140x140.png",
						Url = "http://www.urbandictionary.com/define.php?term=" + e.Arguments.ToString(),
					},
					Description = locale.GetString("miki_module_general_urban_author", entry.Author)
				}.AddField(locale.GetString("miki_module_general_urban_definition"), entry.Definition, true)
				.AddField(locale.GetString("miki_module_general_urban_example"), entry.Example, true)
				.AddField(locale.GetString("miki_module_general_urban_rating"), "👍 " + entry.ThumbsUp + "  👎 " + entry.ThumbsDown, true)
				.Build().QueueToChannel(e.Channel);
			}
			else
			{
				e.ErrorEmbed(e.GetResource("error_term_invalid"))
					.Build().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "whois")]
		public async Task WhoIsAsync(EventContext e)
		{
			ArgObject arg = e.Arguments.Join();

			if (arg == null)
			{
				// TODO: error message
				return;
			}

			IGuildUser user = await arg.GetUserAsync(e.Guild);

			if (user == null)
			{
				// TODO: error message
				return;
			}

			Locale l = new Locale(e.Channel.Id);

			EmbedBuilder embed = Utils.Embed;
			embed.Title = $"Who is {user.Username}!?";
			embed.WithColor(0.5f, 0f, 1.0f);

			embed.ImageUrl = user.GetAvatarUrl();

			embed.AddInlineField(
				l.GetString("miki_module_whois_tag_personal"),
				$"User Id      : **{user.Id}**\nUsername: **{user.Username}#{user.Discriminator} {(string.IsNullOrEmpty(user.Nickname) ? "" : $"({user.Nickname})")}**\nCreated at: **{user.CreatedAt.ToString()}**\nJoined at   : **{user.JoinedAt.ToString()}**\n");

			List<string> roles = new List<string>();
			foreach (ulong i in user.RoleIds)
			{
				roles.Add("`" + user.Guild.GetRole(i).Name + "`");
			}

			string r = string.Join(" ", roles);

			if (r.Length <= 1000)
			{
				embed.AddInlineField(
					l.GetString("miki_module_general_guildinfo_roles"),
					r
				);
			}

			embed.Build().QueueToChannel(e.Channel);
		}
	}
}