using Miki.Bot.Models;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands.Filters;
using Miki.Net.Http;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Miki.Framework.Commands.Scopes.Attributes;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Scopes;
using Miki.Framework.Commands.Scopes.Models;
using Miki.Utility;
using Miki.Services;
using Miki.Services.Scheduling;
using Miki.Accounts;
using Miki.Services.Dailies;

namespace Miki.Modules
{
	[Module("Experimental")]
	internal class DeveloperModule
	{
		[Command("identifyemoji")]
        [RequiresScope("developer")]
		public async Task IdentifyEmojiAsync(IContext e)
		{
			if (DiscordEmoji.TryParse(e.GetArgumentPack().Pack.TakeAll(), out var emote))
			{
				await new EmbedBuilder()
					.SetTitle("Emoji Identified!")
					.AddInlineField("Name", emote.Name)
					.AddInlineField("Id", emote.Id.ToString())
					.AddInlineField("Code", $"`{emote}`")
					.ToEmbed()
                    .QueueAsync(e, e.GetChannel());
            }
        }

		[Command("say")]
        [RequiresScope("developer")]
		public Task SayAsync(IContext e)
		{
			e.GetChannel().QueueMessage(e, null, e.GetArgumentPack().Pack.TakeAll());
			return Task.CompletedTask;
		}
        
        [Command("sayembed")]
        [RequiresScope("developer")]
		public async Task SayEmbedAsync(IContext e)
		{
			EmbedBuilder b = new EmbedBuilder();
			string text = e.GetArgumentPack().Pack.TakeAll();

			b.SetDescription(text);

            await b.ToEmbed().QueueAsync(e, e.GetChannel());
		}

        [Command("fetchpayload")]
        [RequiresScope("developer")]
        public async Task GetScheduledPayloadAsync(IContext e)
        {
            var schedulerService = e.GetService<ISchedulerService>();
            var cache = e.GetService<IExtendedCacheClient>();

            var ownerId = e.GetArgumentPack().TakeRequired<string>();
            var uuid = e.GetArgumentPack().TakeRequired<string>();

            var objectKey = schedulerService.GetObjectNamespace(ownerId);

            var payload = await cache.HashGetAsync<TaskPayload>(objectKey, uuid);

            await e.GetChannel().SendMessageAsync(JsonConvert.SerializeObject(payload));
        }

        [Command("triggerlevelup")]
        [RequiresScope("developer")]
        public class LevelUpCommand
        {
			[Command]
			[RequiresScope("developer")]
            public async Task TriggerLevelUpAsync(IContext e)
            {
                var level = e.GetArgumentPack().TakeRequired<int>();
                var service = e.GetService<AccountService>();
                await service.LevelUpLocalAsync(e.GetMessage(), level);
            }
        }

        [Command("identifyuser")]
        [RequiresScope("developer")]
		public async Task IdenUserAsync(IContext e)
		{
            var api = e.GetService<IDiscordClient>();
            var user = await api.GetUserAsync(ulong.Parse(e.GetArgumentPack().Pack.TakeAll()));

			if (user.Id == 0)
			{
				await e.GetChannel().SendMessageAsync("none.");
			}

			await e.GetChannel().SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

		[Command("identifyguilduser")]
        [RequiresScope("developer")]
		public async Task IdenGuildUserAsync(IContext e)
		{
            var api = e.GetService<IDiscordClient>();
			var user = await api.GetGuildUserAsync(
				ulong.Parse(e.GetArgumentPack().Pack.TakeAll()), e.GetGuild().Id);

			if (user == null)
			{
				await e.GetChannel().SendMessageAsync("none.");
			}

			await e.GetChannel().SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

		[Command("showpermissions")]
        [RequiresScope("developer")]
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

				e.GetChannel().QueueMessage(e, null, x);
			}
		}

		[Command("haspermission")]
        [RequiresScope("developer")]
		public async Task HasPermissionAsync(IContext e)
		{
			var user = await e.GetGuild().GetSelfAsync();
			if(await user.HasPermissionsAsync(
                Enum.Parse<GuildPermission>(e.GetArgumentPack().Pack.TakeAll())))
			{
				e.GetChannel().QueueMessage(e, null, "Yes!");
			}
			else
			{
				e.GetChannel().QueueMessage(e, null, $"No!");
			}
		}

		[Command("identifyguildchannel")]
        [RequiresScope("developer")]
		public async Task IdenGuildChannelAsync(IContext e)
		{
            var api = e.GetService<IDiscordClient>();
			var user = await api.GetChannelAsync(ulong.Parse(e.GetArgumentPack().Pack.TakeAll()));

			if (user == null)
			{
				await e.GetChannel().SendMessageAsync("none.");
			}

			await e.GetChannel().SendMessageAsync($"```json\n{JsonConvert.SerializeObject(user)}```");
		}

		[Command("identifyrole")]
        [RequiresScope("developer")]
		public async Task IdentifyRoleAsync(IContext e)
		{
			if(e.GetArgumentPack().Take(out ulong roleId))
			{
				var x = await e.GetGuild().GetRoleAsync(roleId);
				var myHierarchy = await (await e.GetGuild().GetSelfAsync()).GetHierarchyAsync();

				e.GetChannel().QueueMessage(e, null, "```" + JsonConvert.SerializeObject(new
				{
					role = x,
					bot_position = myHierarchy,
				}) + "```");
			}
		}

		[Command("identifybotroles")]
        [RequiresScope("developer")]
		public async Task IdentifyBotRolesAsync(IContext e)
		{
			var roles = await e.GetGuild().GetRolesAsync();
			var self = await e.GetGuild().GetSelfAsync();
			e.GetChannel().QueueMessage(
                e, 
                null, 
                $"```{JsonConvert.SerializeObject(roles.Where(x => self.RoleIds.Contains(x.Id)))}```");
		}

		[Command("ignore")]
        [RequiresScope("developer")]
        public Task IgnoreIdAsync(IContext e)
		{
            if (e.GetArgumentPack().Take(out ulong id))
            {
                var userFilter = e.GetService<FilterPipelineStage>()
                    .GetFilterOfType<UserFilter>();
                userFilter.Users.Add((long)id);

                e.GetChannel().QueueMessage(e, null, ":ok_hand:");
            }
			return Task.CompletedTask;
		}

		[Command("dev")]
        [RequiresScope("developer.internal")]
        public Task ShowCacheAsync(IContext e)
		{
			e.GetChannel().QueueMessage(e, null, "Yes, this is Veld, my developer.");
			return Task.CompletedTask;
		}

		[Command("setmekos")]
        [RequiresScope("developer")]
        public async Task SetMekosAsync(IContext e)
		{
			if(e.GetArgumentPack().Take(out string userArg))
			{
				IDiscordUser user = await e.GetGuild().FindUserAsync(userArg);

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
					e.GetChannel().QueueMessage(e, null, ":ok_hand:");
				}
			}
		}

		[Command("changeavatar")]
        [RequiresScope("developer.internal")]
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
        [RequiresScope("developer")]
        public async Task CreateKeyAsync(IContext e)
		{
			var context = e.GetService<MikiDbContext>();

			DonatorKey key = (await context.DonatorKey.AddAsync(new DonatorKey()
			{
				StatusTime = new TimeSpan(int.Parse(e.GetArgumentPack().Pack.TakeAll()), 0, 0, 0, 0)
			})).Entity;

			await context.SaveChangesAsync();
			e.GetChannel().QueueMessage(
                e, null, $"Key generated for {key.StatusTime.TotalDays} days `{key.Key}`");
		}

		[Command("setexp")]
        [RequiresScope("developer")]
		public async Task SetExperienceAsync(IContext e)
		{
			var cache = e.GetService<ICacheClient>();

			if(!e.GetArgumentPack().Take(out string userName))
			{
				return;
			}

			var user = await e.GetGuild().FindUserAsync(userName);

			e.GetArgumentPack().Take(out int amount);
			var context = e.GetService<MikiDbContext>();

			var localUser = await LocalExperience.GetAsync(
				context,
				e.GetGuild().Id,
				user.Id);
			if(localUser == null)
			{
				localUser = await LocalExperience.CreateAsync(
					context,
					e.GetGuild().Id,
					user.Id,
					user.Username);
			}

			localUser.Experience = amount;
			await context.SaveChangesAsync();
			await cache.UpsertAsync($"user:{e.GetGuild().Id}:{e.GetAuthor().Id}:exp", localUser.Experience);
			e.GetChannel().QueueMessage(e, null, ":ok_hand:");
		}

		[Command("setglobexp")]
        [RequiresScope("developer")]
        public async Task SetGlobalExpAsync(IContext e)
		{
			var userService = e.GetService<IUserService>();

			if(!e.GetArgumentPack().Take(out string userName))
			{
				return;
			}

			IDiscordUser user = await e.GetGuild().FindUserAsync(userName);

			if(!e.GetArgumentPack().Take(out int amount))
			{
				return;
			}

			User u = await userService.GetOrCreateUserAsync(user)
				.ConfigureAwait(false);

			u.Total_Experience = amount;

			await userService.UpdateUserAsync(u);
            await userService.SaveAsync();

			e.GetChannel().QueueMessage(e, null, ":ok_hand:");
		}

        [Command("addscope")]
        [RequiresScope("developer.internal")]
        public async Task AddScopeAsync(IContext e)
        {
            var scopeStage = e.GetService<ScopeService>();
            var user = await e.GetGuild().FindUserAsync(
                e.GetArgumentPack().TakeRequired<string>());

            e.GetArgumentPack().Take(out string scope);

            await scopeStage.AddScopeAsync(new Scope{
                UserId = (long)user.Id, 
                ScopeId = scope
            });
            e.GetChannel().QueueMessage(e, null, ":ok_hand:");
        }

		[Command("banuser")]
		[RequiresScope("developer")]
		public async Task BanUserAsync(IContext e)
		{
			var u = await e.GetGuild().FindUserAsync(e);
			var context = e.GetService<MikiDbContext>();
			var userService = e.GetService<IUserService>();

			var userObject = await userService.GetUserAsync((long)u.Id);
			await userObject.BanAsync(context);
		}

        [Command("dailyedit")]
        [RequiresScope("developer")]
        public class DailyEditCommand
        {
            [Command]
            [RequiresScope("developer")]
            public async Task DailyEditAsync(IContext e)
            {
                var embed = new EmbedBuilder()
                    .SetTitle(":shield: Daily Edit")
                    .SetDescription("Available commands are `reset [user]` `setstreak <user> <amount>`")
                    .SetColor(85, 172, 238);
                    
                await embed.ToEmbed().QueueAsync(e, e.GetChannel());
            }

            [Command("reset")]
            [RequiresScope("developer")]
            public async Task ResetAsync(IContext e)
            {
                var userService = e.GetService<IUserService>();

                if (!e.GetArgumentPack().Take(out string userArgument))
                {
                    userArgument = e.GetAuthor().Username;
                }

                var discordUser = await e.GetGuild().FindUserAsync(userArgument);
                var user = await userService.GetOrCreateUserAsync(discordUser)
                    .ConfigureAwait(false);

                var dailyService = e.GetService<IDailyService>();
                var daily = await dailyService.GetOrCreateDailyAsync((long)e.GetAuthor().Id)
                    .ConfigureAwait(false);
                await dailyService.UpdateDailyAsync(daily).ConfigureAwait(false);

                daily.LastClaimTime = DateTime.UtcNow.AddHours(-24);

                await dailyService.SaveAsync().ConfigureAwait(false);

                var message = new EmbedBuilder()
                    .SetTitle(":shield: Daily Edit")
                    .SetDescription(
                        userArgument != null
                            ? $"You have reset {user.Name}'s daily!"
                            : "You have reset your daily!")
                    .SetColor(85, 172, 238);

                await message.ToEmbed().QueueAsync(e, e.GetChannel());
            }

            [Command("setstreak")]
            [RequiresScope("developer")]
            public async Task SetStreak(IContext e)
            {
                var userService = e.GetService<IUserService>();

                if (!e.GetArgumentPack().Take(out string userName))
                {
                    await e.ErrorEmbed(
                        "You didn't give a user! Use `>dailyedit setstreak <user> <streak>`")
                        .ToEmbed()
                        .QueueAsync(e, e.GetChannel());
                    return;
                }

                var discordUser = await e.GetGuild().FindUserAsync(userName);
                var user = await userService.GetOrCreateUserAsync(discordUser)
                    .ConfigureAwait(false);

                var dailyService = e.GetService<IDailyService>();
                var daily = await dailyService.GetOrCreateDailyAsync((long)e.GetAuthor().Id)
                    .ConfigureAwait(false);
                await dailyService.UpdateDailyAsync(daily).ConfigureAwait(false);

                if (!e.GetArgumentPack().Take(out int newStreak))
                {
                    await e.ErrorEmbed(
                        $"You didn't give an amount! Use `>dailyedit setstreak {userName} <streak>`")
                        .ToEmbed()
                        .QueueAsync(e, e.GetChannel());
                    return;
                }
                
                daily.CurrentStreak = newStreak;

                if (newStreak > daily.LongestStreak)
                {
                    daily.LongestStreak = newStreak;
                }

                await dailyService.SaveAsync().ConfigureAwait(false);

                var message = new EmbedBuilder()
                    .SetTitle(":shield: Daily Edit")
                    .SetDescription($"{user.Name}'s streak has been set to {daily.CurrentStreak}!")
                    .SetColor(85, 172, 238);

                await message.ToEmbed().QueueAsync(e, e.GetChannel());
            }
        }
    }
}