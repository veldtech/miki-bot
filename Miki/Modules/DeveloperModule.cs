using Miki.Accounts.Achievements.Objects;
using Miki.Bot.Models;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Common.Packets;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Attributes;
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
		[Command("identifyemoji")]
		public async Task IdentifyEmojiAsync(IContext e)
		{
			if (DiscordEmoji.TryParse(e.GetArgumentPack().Pack.TakeAll(), out var emote))
			{
				await new EmbedBuilder()
					.SetTitle("Emoji Identified!")
					.AddInlineField("Name", emote.Name)
					.AddInlineField("Id", emote.Id.ToString())
					//.AddInlineField("Created At", emote.ToString())
					.AddInlineField("Code", "`" + emote.ToString() + "`")
					//.SetThumbnail(emote.Url)
					.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
			}
        }

		[Command("say")]
		public Task SayAsync(IContext e)
		{
			(e.GetChannel() as IDiscordTextChannel).QueueMessage(e.GetArgumentPack().Pack.TakeAll());
			return Task.CompletedTask;
		}

		[Command("sayembed")]
		public async Task SayEmbedAsync(IContext e)
		{
			EmbedBuilder b = new EmbedBuilder();
			string text = e.GetArgumentPack().Pack.TakeAll();

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

            await b.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
		}

		[Command("identifyuser")]
		public async Task IdenUserAsync(IContext e)
		{
            var api = e.GetService<IApiClient>();
            var user = await api.GetUserAsync(ulong.Parse(e.GetArgumentPack().Pack.TakeAll()));

			if (user == null)
			{
				await (e.GetChannel() as IDiscordTextChannel).SendMessageAsync($"none.");
			}

			await (e.GetChannel() as IDiscordTextChannel).SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

		[Command("identifyguilduser")]
		public async Task IdenGuildUserAsync(IContext e)
		{
            var api = e.GetService<IApiClient>();
            var user = await api.GetGuildUserAsync(ulong.Parse(e.GetArgumentPack().Pack.TakeAll()), e.GetGuild().Id);

			if (user == null)
			{
				await (e.GetChannel() as IDiscordTextChannel).SendMessageAsync($"none.");
			}

			await (e.GetChannel() as IDiscordTextChannel).SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

        [Command("showpermissions")]
        public async Task ShowPermissionsAsync(IContext e)
        {
            if(e.GetArgumentPack().Take(out ulong id))
            {
                var member = await e.GetGuild().GetMemberAsync(id);
                var permissions = await e.GetGuild().GetPermissionsAsync(member);

                string x = "";

                foreach(var z in Enum.GetNames(typeof(GuildPermission)))
                {
                    if(permissions.HasFlag(Enum.Parse<GuildPermission>(z)))
                    {
                        x += z + " ";
                    }
                }

                (e.GetChannel() as IDiscordTextChannel).QueueMessage(x);
            }
        }


        [Command("haspermission")]
        public async Task HasPermissionAsync(IContext e)
        {
            var user = await e.GetGuild().GetSelfAsync();
            if(await user.HasPermissionsAsync(Enum.Parse<GuildPermission>(e.GetArgumentPack().Pack.TakeAll())))
            {
                (e.GetChannel() as IDiscordTextChannel).QueueMessage("Yes!");
            }
            else
            {
                (e.GetChannel() as IDiscordTextChannel).QueueMessage($"No!");
            }
        }

		[Command("identifyguildchannel")]
		public async Task IdenGuildChannelAsync(IContext e)
		{
            var api = e.GetService<IApiClient>();
            var user = await api.GetChannelAsync(ulong.Parse(e.GetArgumentPack().Pack.TakeAll()));

			if (user == null)
			{
				await (e.GetChannel() as IDiscordTextChannel).SendMessageAsync($"none.");
			}

			await (e.GetChannel() as IDiscordTextChannel).SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

        [Command("identifyrole")]
        public async Task IdentifyRoleAsync(IContext e)
        {
            if (e.GetArgumentPack().Take(out ulong roleId))
            {
                var x = await e.GetGuild().GetRoleAsync(roleId);
                var myHierarchy = await (await e.GetGuild().GetSelfAsync()).GetHierarchyAsync();

                (e.GetChannel() as IDiscordTextChannel).QueueMessage("```" + JsonConvert.SerializeObject(new
                {
                    role = x,
                    bot_position = myHierarchy
                }) + "```");
            }
        }

        [Command("sendtestachievement")]
        public async Task SendTestAchievementAsync(IContext e)
        {
            await Notification.SendAchievementAsync(
                new ManualAchievement { Name = "test", Icon = "⚙", Points = 0, ParentName = "" },
                e.GetChannel() as IDiscordTextChannel, 
                e.GetAuthor());
        }

        [Command("identifybotroles")]
        public async Task IdentifyBotRolesAsync(IContext e)
        {
            var roles = await e.GetGuild().GetRolesAsync();
            var self = await e.GetGuild().GetSelfAsync();
            (e.GetChannel() as IDiscordTextChannel).QueueMessage($"```{JsonConvert.SerializeObject(roles.Where(x => self.RoleIds.Contains(x.Id)))}```");
        }

        [Command("setactivity")]
		public async Task SetGameAsync(IContext e)
		{
            if (!e.GetArgumentPack().Take(out string arg))
            {
                return;
            }

			ActivityType type = arg.FromEnum(ActivityType.Playing);

            string text = e.GetArgumentPack().Pack.TakeAll();
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

		[Command("ignore")]
		public Task IgnoreIdAsync(IContext e)
		{
			if (ulong.TryParse(e.GetArgumentPack().Pack.TakeAll(), out ulong id))
			{
				//e.EventSystem.MessageFilter.Get<UserFilter>().Users.Add(id);
				(e.GetChannel() as IDiscordTextChannel).QueueMessage(":ok_hand:");
			}
			return Task.CompletedTask;
		}

		[Command("dev")]
		public Task ShowCacheAsync(IContext e)
		{
			(e.GetChannel() as IDiscordTextChannel).QueueMessage("Yes, this is Veld, my developer.");
			return Task.CompletedTask;
		}

        //[Command(Name = "changeavatar", Accessibility = EventAccessibility.DEVELOPERONLY)]
        //public async Task ChangeAvatarAsync(EventContext e)
        //{
        //	using (Stream s = new FileStream("./" + e.GetArgumentPack().Pack.TakeAll(), FileMode.Open))
        //	{
        //		await Bot.Instance.Client.GetShardFor(e.GetGuild()).CurrentUser.ModifyAsync(z =>
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

        //	embed.Build().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);

        //	await Task.Yield();
        //}

		[Command("presence")]
		public async Task PresenceTestAsync(IContext e)
		{
			IDiscordPresence presence = await e.GetAuthor().GetPresenceAsync();

			var embed = new EmbedBuilder()
				.SetTitle($"{e.GetAuthor().Username} - {presence.Status}")
				.SetThumbnail(e.GetAuthor().GetAvatarUrl());

			if (presence.Activity != null)
			{
				embed.SetDescription($"{presence.Activity.Name} - {presence.Activity.Details ?? ""}\n{presence.Activity.State ?? ""}");
			}

            await embed.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
		}

        [Command("setmekos")]
        public async Task SetMekos(IContext e)
        {
            if (e.GetArgumentPack().Take(out string userArg))
            {
                IDiscordUser user = await DiscordExtensions.GetUserAsync(userArg, e.GetGuild());

                if (e.GetArgumentPack().Take(out int value))
                {
                    var context = e.GetService<MikiDbContext>();

                    User u = await context.Users.FindAsync((long)user.Id);
                    if (u == null)
                    {
                        return;
                    }
                    u.Currency = value;
                    await context.SaveChangesAsync();
                    (e.GetChannel() as IDiscordTextChannel).QueueMessage(":ok_hand:");
                }
            }
        }

        [Command("createkey")]
        public async Task CreateKeyAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            DonatorKey key = (await context.DonatorKey.AddAsync(new DonatorKey()
            {
                StatusTime = new TimeSpan(int.Parse(e.GetArgumentPack().Pack.TakeAll()), 0, 0, 0, 0)
            })).Entity;

            await context.SaveChangesAsync();
            (e.GetChannel() as IDiscordTextChannel).QueueMessage($"key generated for {e.GetArgumentPack().Pack.TakeAll()} days `{key.Key}`");
        }

        [Command("setexp")]
        public async Task SetExp(IContext e)
        {
            var cache = e.GetService<ICacheClient>();

            if (!e.GetArgumentPack().Take(out string userName))
            {
                return;
            }

            IDiscordUser user = await DiscordExtensions.GetUserAsync(userName, e.GetGuild());

            e.GetArgumentPack().Take(out int amount);
            var context = e.GetService<MikiDbContext>();

            LocalExperience u = await LocalExperience.GetAsync(context, e.GetGuild().Id.ToDbLong(), user.Id.ToDbLong(), user.Username);
            if (u == null)
            {
                return;
            }

            u.Experience = amount;
            await context.SaveChangesAsync();
            await cache.UpsertAsync($"user:{e.GetGuild().Id}:{e.GetAuthor().Id}:exp", u.Experience);
            (e.GetChannel() as IDiscordTextChannel).QueueMessage(":ok_hand:");
        }

        [Command("setglobexp")]
        public async Task SetGlobalExpAsync(IContext e)
        {
            if (!e.GetArgumentPack().Take(out string userName))
            {
                return;
            }

            IDiscordUser user = await DiscordExtensions.GetUserAsync(userName, e.GetGuild());

            if (!e.GetArgumentPack().Take(out int amount))
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
            (e.GetChannel() as IDiscordTextChannel).QueueMessage(":ok_hand:");
        }

        [Command("banuser")]
        public async Task BanUserAsync(IContext e)
        {
            if (e.GetArgumentPack().Take(out string user))
            {
                IDiscordUser u = await DiscordExtensions.GetUserAsync(user, e.GetGuild());

                var context = e.GetService<MikiDbContext>();
                await (await User.GetAsync(context, u.Id.ToDbLong(), u.Username))
                    .BanAsync(context);
            }
        }
	}
}