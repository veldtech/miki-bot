using Miki.Bot.Models;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Common.Packets;
using Miki.Framework;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Commands.Filters;
using Miki.Net.Http;
using Newtonsoft.Json;
using System;
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
			if(DiscordEmoji.TryParse(e.GetArgumentPack().Pack.TakeAll(), out var emote))
			{
				await new EmbedBuilder()
					.SetTitle("Emoji Identified!")
					.AddInlineField("Name", emote.Name)
					.AddInlineField("Id", emote.Id.ToString())
					//.AddInlineField("Created At", emote.ToString())
					.AddInlineField("Code", "`" + emote.ToString() + "`")
					//.SetThumbnail(emote.Url)
					.ToEmbed()
					.QueueAsync(e.GetChannel());
			}
		}

		[Command("say")]
		public Task SayAsync(IContext e)
		{
			e.GetChannel()
				.QueueMessage(e.GetArgumentPack().Pack.TakeAll());
			return Task.CompletedTask;
		}

		[Command("sayembed")]
		public async Task SayEmbedAsync(IContext e)
		{
			EmbedBuilder b = new EmbedBuilder();
			string text = e.GetArgumentPack().Pack.TakeAll();

			b.SetDescription(text);

			await b.ToEmbed().QueueAsync(e.GetChannel());
		}

		[Command("identifyuser")]
		public async Task IdenUserAsync(IContext e)
		{
			var api = e.GetService<IApiClient>();
			var user = await api.GetUserAsync(ulong.Parse(e.GetArgumentPack().Pack.TakeAll()));

			if(user.Id == 0)
			{
				await e.GetChannel().SendMessageAsync($"none.");
			}

			await e.GetChannel().SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

		[Command("identifyguilduser")]
		public async Task IdenGuildUserAsync(IContext e)
		{
			var api = e.GetService<IApiClient>();
			var user = await api.GetGuildUserAsync(ulong.Parse(e.GetArgumentPack().Pack.TakeAll()), e.GetGuild().Id);

			if(user == null)
			{
				await e.GetChannel().SendMessageAsync($"none.");
			}

			await e.GetChannel().SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

		[Command("showpermissions")]
		public async Task ShowPermissionsAsync(IContext e)
		{
			if(e.GetArgumentPack().Take(out ulong id))
			{
				var member = await e.GetGuild().GetMemberAsync(id);
				var permissions = await e.GetGuild().GetPermissionsAsync(member);

				string x = string.Empty;

				foreach(var z in Enum.GetNames(typeof(GuildPermission)))
				{
					if(permissions.HasFlag(Enum.Parse<GuildPermission>(z)))
					{
						x += z + " ";
					}
				}

				e.GetChannel().QueueMessage(x);
			}
		}

		[Command("haspermission")]
		public async Task HasPermissionAsync(IContext e)
		{
			var user = await e.GetGuild().GetSelfAsync();
			if(await user.HasPermissionsAsync(Enum.Parse<GuildPermission>(e.GetArgumentPack().Pack.TakeAll())))
			{
				e.GetChannel().QueueMessage("Yes!");
			}
			else
			{
				e.GetChannel().QueueMessage($"No!");
			}
		}

		[Command("identifyguildchannel")]
		public async Task IdenGuildChannelAsync(IContext e)
		{
			var api = e.GetService<IApiClient>();
			var user = await api.GetChannelAsync(ulong.Parse(e.GetArgumentPack().Pack.TakeAll()));

			if(user == null)
			{
				await e.GetChannel().SendMessageAsync($"none.");
			}

			await e.GetChannel().SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

		[Command("identifyrole")]
		public async Task IdentifyRoleAsync(IContext e)
		{
            /*if (!e.GetArgumentPack().Take(out string arg))
            {
                return;
            }

			ActivityType type = arg.FromEnum(ActivityType.Playing);

			string text = e.GetArgumentPack().Pack.TakeAll();
			string url = null;

			if(type == ActivityType.Streaming)
				url = "https://twitch.tv/velddev";

			for(int i = 0; i < Global.Config.ShardCount; i++)
			{
				await e.GetService<DiscordClient>()
					.SetGameAsync(i, new DiscordStatus
					{
						Name = text,
						Url = url,
						Type = type
					},
					Status = "online",
					IsAFK = false
				});
			}*/
		}

		[Command("ignore")]
		public Task IgnoreIdAsync(IContext e)
		{
			if(e.GetArgumentPack().Take(out ulong id))
			{
				var userFilter = e.GetService<FilterPipelineStage>()
					.GetFilterOfType<UserFilter>();
				userFilter.Users.Add((long)id);

				e.GetChannel().QueueMessage(":ok_hand:");
			}
			return Task.CompletedTask;
		}

		[Command("dev")]
		public Task ShowCacheAsync(IContext e)
		{
			e.GetChannel().QueueMessage("Yes, this is Veld, my developer.");
			return Task.CompletedTask;
		}

		[Command("setmekos")]
		public async Task SetMekosAsync(IContext e)
		{
			if(e.GetArgumentPack().Take(out string userArg))
			{
				IDiscordUser user = await DiscordExtensions.GetUserAsync(userArg, e.GetGuild());

				if(e.GetArgumentPack().Take(out int value))
				{
					var context = e.GetService<MikiDbContext>();

					User u = await context.Users.FindAsync((long)user.Id);
					if(u == null)
					{
						return;
					}
					u.Currency = value;
					await context.SaveChangesAsync();
					e.GetChannel().QueueMessage(":ok_hand:");
				}
			}
		}

		[Command("changeavatar")]
		public async Task ChangeAvatarAsync(IContext e)
		{
			var s = e.GetMessage().Attachments.FirstOrDefault();
			var stream = await new HttpClient(s.Url).GetStreamAsync();
			var client = e.GetService<DiscordClient>();
			var self = await client.GetSelfAsync();
			await self.ModifyAsync(x =>
			{
				x.Avatar = stream;
			});
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
			e.GetChannel().QueueMessage($"key generated for {e.GetArgumentPack().Pack.TakeAll()} days `{key.Key}`");
		}

		[Command("setexp")]
		public async Task SetExperienceAsync(IContext e)
		{
			var cache = e.GetService<ICacheClient>();

			if(!e.GetArgumentPack().Take(out string userName))
			{
				return;
			}

			IDiscordUser user = await DiscordExtensions.GetUserAsync(userName, e.GetGuild());

			e.GetArgumentPack().Take(out int amount);
			var context = e.GetService<MikiDbContext>();

			LocalExperience u = await LocalExperience.GetAsync(
				context,
				e.GetGuild().Id,
				user.Id);
			if(u == null)
			{
				u = await LocalExperience.CreateAsync(
					context,
					e.GetGuild().Id,
					user.Id,
					user.Username);
			}

			u.Experience = amount;
			await context.SaveChangesAsync();
			await cache.UpsertAsync($"user:{e.GetGuild().Id}:{e.GetAuthor().Id}:exp", u.Experience);
			e.GetChannel().QueueMessage(":ok_hand:");
		}

		[Command("setglobexp")]
		public async Task SetGlobalExpAsync(IContext e)
		{
			if(!e.GetArgumentPack().Take(out string userName))
			{
				return;
			}

			IDiscordUser user = await DiscordExtensions.GetUserAsync(userName, e.GetGuild());

			if(!e.GetArgumentPack().Take(out int amount))
			{
				return;
			}
			var context = e.GetService<MikiDbContext>();

			User u = await User.GetAsync(context, user.Id.ToDbLong(), user.Username);
			if(u == null)
			{
				return;
			}
			u.Total_Experience = amount;
			await context.SaveChangesAsync();
			e.GetChannel().QueueMessage(":ok_hand:");
		}

		[Command("banuser")]
		public async Task BanUserAsync(IContext e)
		{
			if(e.GetArgumentPack().Take(out string user))
			{
				IDiscordUser u = await DiscordExtensions.GetUserAsync(user, e.GetGuild());

				var context = e.GetService<MikiDbContext>();
				await (await User.GetAsync(context, u.Id.ToDbLong(), u.Username))
					.BanAsync(context);
			}
		}
	}
}