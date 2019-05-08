using Miki.Accounts.Achievements.Objects;
using Miki.Bot.Models;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Common.Packets;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Commands.Permissions;
using Miki.Framework.Commands.Permissions.Attributes;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
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
        [RequiresPermission(PermissionLevel.STAFF)]
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
        [RequiresPermission(PermissionLevel.STAFF)]
        public Task SayAsync(IContext e)
		{
			(e.GetChannel() as IDiscordTextChannel).QueueMessage(e.GetArgumentPack().Pack.TakeAll());
			return Task.CompletedTask;
		}

		[Command("sayembed")]
        [RequiresPermission(PermissionLevel.STAFF)]
        public async Task SayEmbedAsync(IContext e)
		{
			EmbedBuilder b = new EmbedBuilder();
			string text = e.GetArgumentPack().Pack.TakeAll();

			b.SetDescription(text);

            await b.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
		}

		[Command("identifyuser")]
        [RequiresPermission(PermissionLevel.STAFF)]
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
        [RequiresPermission(PermissionLevel.STAFF)]
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
        [RequiresPermission(PermissionLevel.STAFF)]
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
        [RequiresPermission(PermissionLevel.STAFF)]
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
        [RequiresPermission(PermissionLevel.STAFF)]
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
        [RequiresPermission(PermissionLevel.STAFF)]
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

        [Command("identifybotroles")]
        [RequiresPermission(PermissionLevel.STAFF)]
        public async Task IdentifyBotRolesAsync(IContext e)
        {
            var roles = await e.GetGuild().GetRolesAsync();
            var self = await e.GetGuild().GetSelfAsync();
            (e.GetChannel() as IDiscordTextChannel).QueueMessage($"```{JsonConvert.SerializeObject(roles.Where(x => self.RoleIds.Contains(x.Id)))}```");
        }

        [Command("setactivity")]
        [RequiresPermission(PermissionLevel.DEVELOPER)]
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
        [RequiresPermission(PermissionLevel.DEVELOPER)]
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
        [RequiresPermission(PermissionLevel.DEVELOPER)]
        public Task ShowCacheAsync(IContext e)
		{
			(e.GetChannel() as IDiscordTextChannel).QueueMessage("Yes, this is Veld, my developer.");
			return Task.CompletedTask;
		}

        [Command("setmekos")]
        [RequiresPermission(PermissionLevel.DEVELOPER)]
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
        [RequiresPermission(PermissionLevel.DEVELOPER)]
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
        [RequiresPermission(PermissionLevel.DEVELOPER)]
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
        [RequiresPermission(PermissionLevel.DEVELOPER)]
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
        [RequiresPermission(PermissionLevel.DEVELOPER)]
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