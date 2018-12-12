using Miki.API.EmbedMenus;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Common.Packets;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Framework.Events.Filters;
using Miki.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
	[Module("Experimental")]
	internal class DeveloperModule
	{
		[Command(Name = "identifyemoji", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public Task IdentifyEmojiAsync(EventContext e)
		{
			if (DiscordEmoji.TryParse(e.Arguments.ToString(), out var emote))
			{
				new EmbedBuilder()
					.SetTitle("Emoji Identified!")
					.AddInlineField("Name", emote.Name)
					.AddInlineField("Id", emote.Id.ToString())
					//.AddInlineField("Created At", emote.ToString())
					.AddInlineField("Code", "`" + emote.ToString() + "`")
					//.SetThumbnail(emote.Url)
					.ToEmbed().QueueToChannel(e.Channel);
			}
			return Task.CompletedTask;
		}

		[Command(Name = "say", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public Task SayAsync(EventContext e)
		{
			e.Channel.QueueMessageAsync(e.Arguments.ToString());
			return Task.CompletedTask;
		}

		[Command(Name = "sayembed", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public Task SayEmbedAsync(EventContext e)
		{
			EmbedBuilder b = new EmbedBuilder();
			string text = e.Arguments.ToString();

			//if (e.message.Attachments.Count == 0)
			//{
			//	Match m = Regex.Match(e.message.Content, "(http(s)?://)(i.)?(imgur.com)/([A-Za-z0-9]+)(.png|.gif(v)?)");
			//	if(m.Success)
			//	{
			//		text = text.Replace(m.Value, "");
			//		b.SetImage(m.Value);
			//	}
			//}
			//else
			//{
			//	b.SetImage(e.message.Attachments.First().Url);
			//}

			b.SetDescription(text);

			b.ToEmbed().QueueToChannel(e.Channel);

			return Task.CompletedTask;
		}

		[Command(Name = "identifyuser", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task IdenUserAsync(EventContext e)
		{
			var user = await Global.ApiClient.GetUserAsync(ulong.Parse(e.Arguments.ToString()));

			if (user == null)
			{
				await (e.Channel as IDiscordTextChannel).SendMessageAsync($"none.");
			}

			await (e.Channel as IDiscordTextChannel).SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

		[Command(Name = "identifyguilduser", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task IdenGuildUserAsync(EventContext e)
		{
			var user = await Global.ApiClient.GetGuildUserAsync(ulong.Parse(e.Arguments.ToString()), e.Guild.Id);

			if (user == null)
			{
				await (e.Channel as IDiscordTextChannel).SendMessageAsync($"none.");
			}

			await (e.Channel as IDiscordTextChannel).SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

		[Command(Name = "identifyguildchannel", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task IdenGuildChannelAsync(EventContext e)
		{
			var user = await Global.ApiClient.GetChannelAsync(ulong.Parse(e.Arguments.ToString()));

			if (user == null)
			{
				await (e.Channel as IDiscordTextChannel).SendMessageAsync($"none.");
			}

			await (e.Channel as IDiscordTextChannel).SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

		[Command(Name = "setactivity", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task SetGameAsync(EventContext e)
		{
			var arg = e.Arguments.FirstOrDefault();

			if (arg == null)
				return;

			ActivityType type = arg.FromEnum(ActivityType.Playing);

			arg = arg.Next();

			string text = arg.TakeUntilEnd().Argument;
			string url = null;

			if (type == ActivityType.Streaming)
				url = "https://twitch.tv/velddev";

			for (int i = 0; i < Global.Config.ShardCount; i++)
			{
				await Global.Client.Discord.SetGameAsync(i, new DiscordStatus
				{
					Game = new Discord.Common.Packets.Activity
					{
						Name = text,
						Url = url,
						Type = type
					},
					Status = "online",
					IsAFK = false
				});
			}
		}

		[Command(Name = "ignore", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public Task IgnoreIdAsync(EventContext e)
		{
			if (ulong.TryParse(e.Arguments.ToString(), out ulong id))
			{
				e.EventSystem.MessageFilter.Get<UserFilter>().Users.Add(id);
				e.Channel.QueueMessageAsync(":ok_hand:");
			}
			return Task.CompletedTask;
		}

		[Command(Name = "dev", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public Task ShowCacheAsync(EventContext e)
		{
			e.Channel.QueueMessageAsync("Yes, this is Veld, my developer.");
			return Task.CompletedTask;
		}

		//[Command(Name = "changeavatar", Accessibility = EventAccessibility.DEVELOPERONLY)]
		//public async Task ChangeAvatarAsync(EventContext e)
		//{
		//	using (Stream s = new FileStream("./" + e.Arguments.ToString(), FileMode.Open))
		//	{
		//		await Bot.Instance.Client.GetShardFor(e.Guild).CurrentUser.ModifyAsync(z =>
		//		{
		//			z.Avatar = new Image(s);
		//		});
		//	}
		//}

		//[Command(Name = "dumpshards", Accessibility = EventAccessibility.DEVELOPERONLY, Aliases = new string[] { "ds" })]
		//public async Task DumpShards(EventContext e)
		//{
		//	EmbedBuilder embed = Utils.Embed;
		//	embed.Title = "Shards";

		//	for (int i = 0; i < (int)Math.Ceiling((double)Bot.Instance.Client.Shards.Count / 20); i++)
		//	{
		//		string title = $"{i * 20} - {(i + 1) * 20}";
		//		string content = "";
		//		for (int j = i * 20; j < Math.Min(i * 20 + 20, Bot.Instance.Client.Shards.Count); j++)
		//		{
		//			DiscordSocketClient c = Bot.Instance.Client.Shards.ElementAt(j);

		//			content += $"`Shard {c.ShardId.ToString().PadRight(2)}` | `State: {c.ConnectionState} Ping: {c.Latency} Guilds: {c.Guilds.Count}`\n";
		//		}
		//		embed.AddInlineField(title, content);
		//	}

		//	embed.Build().QueueToChannel(e.Channel);

		//	await Task.Yield();
		//}

		[Command(Name = "menutest", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task MenuTestAsync(EventContext context)
		{
			await new Menu(m =>
			{
				m.Owner = context.Author;
				m.Root = new SubMenuItem()
				{
					MenuInstance = m,
					Parent = null,
					name = "Test menu",
					children = new List<IMenuItem>()
					{
						new SubMenuItem()
						{
							MenuInstance = m,
							Parent = m.Root,
							name = "Option 1",
							children = null,
						},
						new SubMenuItem()
						{
							MenuInstance = m,
							Parent = m.Root,
							name = "Option 2",
							children = null,
						},
						new PreviewItem()
						{
							MenuInstance = m,
							Parent = m.Root,
							name = "Show me an image",
							imageUrl = "https://cdn.discordapp.com/attachments/431318403119185931/431543971823484938/unknown.png",
						}
					},
				};
			}).StartAsync(context.Channel);
		}

		[Command(Name = "spellcheck", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task SpellCheckAsync(EventContext context)
		{
			EmbedBuilder embed = new EmbedBuilder
			{
				Title = "Spellcheck - top results"
			};

			API.StringComparison.StringComparer sc = new API.StringComparison.StringComparer(context.EventSystem.GetCommandHandler<SimpleCommandHandler>().Commands.Select(z => z.Name));
			List<API.StringComparison.StringComparison> best = sc.CompareToAll(context.Arguments.ToString())
																 .OrderBy(z => z.score)
																 .ToList();
			int x = 1;

			foreach (API.StringComparison.StringComparison c in best)
			{
				embed.AddInlineField($"#{x}", c.ToString());
				x++;
				if (x > 16)
					break;
			}

			embed.ToEmbed().QueueToChannel(context.Channel);

			await Task.Yield();
		}

		[Command(Name = "setdonator", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task SetDonator(EventContext context)
		{
			using (MikiContext database = new MikiContext())
			{
				if (context.message.MentionedUserIds.Count > 0)
				{
					Achievement a = await database.Achievements.FindAsync(context.message.MentionedUserIds.First().ToDbLong(), "donator");
					if (a == null)
					{
						database.Achievements.Add(new Achievement() { Id = context.message.MentionedUserIds.First().ToDbLong(), Name = "donator", Rank = 0 });
						await database.SaveChangesAsync();
					}
				}
				else
				{
					ulong.TryParse(context.message.Content, out ulong x);
					if (x != 0)
					{
						database.Achievements.Add(new Achievement() { Id = x.ToDbLong(), Name = "donator", Rank = 0 });
						await database.SaveChangesAsync();
					}
				}
				context.Channel.QueueMessageAsync(":ok_hand:");
			}
		}

		[Command(Name = "presence", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task PresenceTestAsync(EventContext e)
		{
			IDiscordPresence presence = await e.Author.GetPresenceAsync();

			var embed = new EmbedBuilder()
				.SetTitle($"{e.Author.Username} - {presence.Status}")
				.SetThumbnail(e.Author.GetAvatarUrl());

			if (presence.Activity != null)
			{
				embed.SetDescription($"{presence.Activity.Name} - {presence.Activity.Details ?? ""}\n{presence.Activity.State ?? ""}");
			}

			embed.ToEmbed().QueueToChannel(e.Channel);
		}

		[Command(Name = "setmekos", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task SetMekos(EventContext e)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();

			IDiscordUser user = await arg.GetUserAsync(e.Guild);

			arg = arg.Next();

			int amount = arg?.TakeInt() ?? 0;

			using (var context = new MikiContext())
			{
				User u = await context.Users.FindAsync((long)user.Id);
				if (u == null)
				{
					return;
				}
				u.Currency = amount;
				await context.SaveChangesAsync();
				e.Channel.QueueMessageAsync(":ok_hand:");
			}
		}

		[Command(Name = "createkey", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task CreateKeyAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				DonatorKey key = (await context.DonatorKey.AddAsync(new DonatorKey()
				{
					StatusTime = new TimeSpan(int.Parse(e.Arguments.ToString()), 0, 0, 0, 0)
				})).Entity;

				await context.SaveChangesAsync();
				e.Channel.QueueMessageAsync($"key generated for {e.Arguments.ToString()} days `{key.Key}`");
			}
		}

		[Command(Name = "setexp", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task SetExp(EventContext e)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();

			IDiscordUser user = await arg.GetUserAsync(e.Guild);

			arg = arg.Next();

			int amount = arg?.TakeInt() ?? 0;

			using (var context = new MikiContext())
			{
				LocalExperience u = await LocalExperience.GetAsync(context, e.Guild.Id.ToDbLong(), user.Id.ToDbLong(), user.Username);
				if (u == null)
				{
					return;
				}
				u.Experience = amount;
				await context.SaveChangesAsync();
				await Global.RedisClient.UpsertAsync($"user:{e.Guild.Id}:{e.Author.Id}:exp", u.Experience);
				e.Channel.QueueMessageAsync(":ok_hand:");
			}
		}

		[Command(Name = "banuser", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task BanUserAsync(EventContext e)
		{
			IDiscordUser u = await e.Arguments.First().GetUserAsync(e.Guild);

			using (var context = new MikiContext())
			{
				await (await User.GetAsync(context, u.Id.ToDbLong(), u.Username)).BanAsync();
			}
		}
	}
}