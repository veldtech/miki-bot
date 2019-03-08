using Miki.Bot.Models;
using Miki.Cache;
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
		public async Task IdentifyEmojiAsync(CommandContext e)
		{
			if (DiscordEmoji.TryParse(e.Arguments.Pack.TakeAll(), out var emote))
			{
				await new EmbedBuilder()
					.SetTitle("Emoji Identified!")
					.AddInlineField("Name", emote.Name)
					.AddInlineField("Id", emote.Id.ToString())
					//.AddInlineField("Created At", emote.ToString())
					.AddInlineField("Code", "`" + emote.ToString() + "`")
					//.SetThumbnail(emote.Url)
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
        }

		[Command(Name = "say", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public Task SayAsync(CommandContext e)
		{
			e.Channel.QueueMessage(e.Arguments.Pack.TakeAll());
			return Task.CompletedTask;
		}

		[Command(Name = "sayembed", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task SayEmbedAsync(CommandContext e)
		{
			EmbedBuilder b = new EmbedBuilder();
			string text = e.Arguments.Pack.TakeAll();

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

            await b.ToEmbed().QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "identifyuser", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task IdenUserAsync(CommandContext e)
		{
            var api = e.GetService<IApiClient>();
            var user = await api.GetUserAsync(ulong.Parse(e.Arguments.Pack.TakeAll()));

			if (user == null)
			{
				await (e.Channel as IDiscordTextChannel).SendMessageAsync($"none.");
			}

			await (e.Channel as IDiscordTextChannel).SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

		[Command(Name = "identifyguilduser", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task IdenGuildUserAsync(CommandContext e)
		{
            var api = e.GetService<IApiClient>();
            var user = await api.GetGuildUserAsync(ulong.Parse(e.Arguments.Pack.TakeAll()), e.Guild.Id);

			if (user == null)
			{
				await (e.Channel as IDiscordTextChannel).SendMessageAsync($"none.");
			}

			await (e.Channel as IDiscordTextChannel).SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

        [Command(Name = "showpermissions", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task ShowPermissionsAsync(CommandContext e)
        {
            if(e.Arguments.Take(out ulong id))
            {
                var member = await e.Guild.GetMemberAsync(id);
                var permissions = await e.Guild.GetPermissionsAsync(member);

                string x = "";

                foreach(var z in Enum.GetNames(typeof(GuildPermission)))
                {
                    if(permissions.HasFlag(Enum.Parse<GuildPermission>(z)))
                    {
                        x += z + " ";
                    }
                }

                e.Channel.QueueMessage(x);
            }
        }


        [Command(Name = "haspermission", Accessibility =EventAccessibility.DEVELOPERONLY)]
        public async Task HasPermissionAsync(CommandContext e)
        {
            var user = await e.Guild.GetSelfAsync();
            if(await user.HasPermissionsAsync(Enum.Parse<GuildPermission>(e.Arguments.Pack.TakeAll())))
            {
                e.Channel.QueueMessage("Yes!");
            }
            else
            {
                e.Channel.QueueMessage($"No!");
            }
        }

		[Command(Name = "identifyguildchannel", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task IdenGuildChannelAsync(CommandContext e)
		{
            var api = e.GetService<IApiClient>();
            var user = await api.GetChannelAsync(ulong.Parse(e.Arguments.Pack.TakeAll()));

			if (user == null)
			{
				await (e.Channel as IDiscordTextChannel).SendMessageAsync($"none.");
			}

			await (e.Channel as IDiscordTextChannel).SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

        [Command(Name = "identifyrole", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task IdentifyRoleAsync(CommandContext e)
        {
            if (e.Arguments.Take(out ulong roleId))
            {
                var x = await e.Guild.GetRoleAsync(roleId);
                var myHierarchy = await (await e.Guild.GetSelfAsync()).GetHierarchyAsync();

                e.Channel.QueueMessage("```" + JsonConvert.SerializeObject(new
                {
                    role = x,
                    bot_position = myHierarchy
                }) + "```");
            }
        }

        [Command(Name = "identifybotroles", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task IdentifyBotRolesAsync(CommandContext e)
        {
            var roles = await e.Guild.GetRolesAsync();
            var self = await e.Guild.GetSelfAsync();
            e.Channel.QueueMessage($"```{JsonConvert.SerializeObject(roles.Where(x => self.RoleIds.Contains(x.Id)))}```");
        }

        [Command(Name = "setactivity", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task SetGameAsync(CommandContext e)
		{
            if (!e.Arguments.Take(out string arg))
            {
                return;
            }

			ActivityType type = arg.FromEnum(ActivityType.Playing);

            string text = e.Arguments.Pack.TakeAll();
			string url = null;

			if (type == ActivityType.Streaming)
				url = "https://twitch.tv/velddev";

			for (int i = 0; i < Global.Config.ShardCount; i++)
			{
				await MikiApp.Instance.Discord.SetGameAsync(i, new DiscordStatus
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
		public Task IgnoreIdAsync(CommandContext e)
		{
			if (ulong.TryParse(e.Arguments.Pack.TakeAll(), out ulong id))
			{
				e.EventSystem.MessageFilter.Get<UserFilter>().Users.Add(id);
				e.Channel.QueueMessage(":ok_hand:");
			}
			return Task.CompletedTask;
		}

		[Command(Name = "dev", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public Task ShowCacheAsync(CommandContext e)
		{
			e.Channel.QueueMessage("Yes, this is Veld, my developer.");
			return Task.CompletedTask;
		}

        //[Command(Name = "changeavatar", Accessibility = EventAccessibility.DEVELOPERONLY)]
        //public async Task ChangeAvatarAsync(EventContext e)
        //{
        //	using (Stream s = new FileStream("./" + e.Arguments.Pack.TakeAll(), FileMode.Open))
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

        //	embed.Build().QueueToChannelAsync(e.Channel);

        //	await Task.Yield();
        //}

		[Command(Name = "spellcheck", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task SpellCheckAsync(CommandContext context)
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

            await embed.ToEmbed().QueueToChannelAsync(context.Channel);

			await Task.Yield();
		}

		[Command(Name = "presence", Accessibility = EventAccessibility.DEVELOPERONLY)]
		public async Task PresenceTestAsync(CommandContext e)
		{
			IDiscordPresence presence = await e.Author.GetPresenceAsync();

			var embed = new EmbedBuilder()
				.SetTitle($"{e.Author.Username} - {presence.Status}")
				.SetThumbnail(e.Author.GetAvatarUrl());

			if (presence.Activity != null)
			{
				embed.SetDescription($"{presence.Activity.Name} - {presence.Activity.Details ?? ""}\n{presence.Activity.State ?? ""}");
			}

            await embed.ToEmbed().QueueToChannelAsync(e.Channel);
		}

        [Command(Name = "setmekos", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task SetMekos(CommandContext e)
        {
            if (e.Arguments.Take(out string userArg))
            {
                IDiscordUser user = await DiscordExtensions.GetUserAsync(userArg, e.Guild);

                if (e.Arguments.Take(out int value))
                {
                    var context = e.GetService<MikiDbContext>();

                    User u = await context.Users.FindAsync((long)user.Id);
                    if (u == null)
                    {
                        return;
                    }
                    u.Currency = value;
                    await context.SaveChangesAsync();
                    e.Channel.QueueMessage(":ok_hand:");
                }
            }
        }

        [Command(Name = "createkey", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task CreateKeyAsync(CommandContext e)
        {
            var context = e.GetService<MikiDbContext>();

            DonatorKey key = (await context.DonatorKey.AddAsync(new DonatorKey()
            {
                StatusTime = new TimeSpan(int.Parse(e.Arguments.Pack.TakeAll()), 0, 0, 0, 0)
            })).Entity;

            await context.SaveChangesAsync();
            e.Channel.QueueMessage($"key generated for {e.Arguments.Pack.TakeAll()} days `{key.Key}`");
        }

        [Command(Name = "setexp", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task SetExp(CommandContext e)
        {
            var cache = e.GetService<ICacheClient>();

            if (!e.Arguments.Take(out string userName))
            {
                return;
            }

            IDiscordUser user = await DiscordExtensions.GetUserAsync(userName, e.Guild);

            e.Arguments.Take(out int amount);
            var context = e.GetService<MikiDbContext>();

            LocalExperience u = await LocalExperience.GetAsync(context, e.Guild.Id.ToDbLong(), user.Id.ToDbLong(), user.Username);
            if (u == null)
            {
                return;
            }

            u.Experience = amount;
            await context.SaveChangesAsync();
            await cache.UpsertAsync($"user:{e.Guild.Id}:{e.Author.Id}:exp", u.Experience);
            e.Channel.QueueMessage(":ok_hand:");
        }

        [Command(Name = "setglobexp", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task SetGlobalExpAsync(CommandContext e)
        {
            if (!e.Arguments.Take(out string userName))
            {
                return;
            }

            IDiscordUser user = await DiscordExtensions.GetUserAsync(userName, e.Guild);

            if (!e.Arguments.Take(out int amount))
            {
                return;
            }
            var context = e.GetService<MikiDbContext>();

            User u = await User.GetAsync(context, user.Id.ToDbLong(), user.Username);
            if (u == null)
            {
                return;
            }
            u.Total_Experience = amount;
            await context.SaveChangesAsync();
            e.Channel.QueueMessage(":ok_hand:");
        }

        [Command(Name = "banuser", Accessibility = EventAccessibility.DEVELOPERONLY)]
        public async Task BanUserAsync(CommandContext e)
        {
            if (e.Arguments.Take(out string user))
            {
                IDiscordUser u = await DiscordExtensions.GetUserAsync(user, e.Guild);

                var context = e.GetService<MikiDbContext>();
                await (await User.GetAsync(context, u.Id.ToDbLong(), u.Username))
                    .BanAsync(context);
            }
        }
	}
}