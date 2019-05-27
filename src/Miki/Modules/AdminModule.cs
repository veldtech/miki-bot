using Miki.Bot.Models;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Commands.Permissions;
using Miki.Framework.Commands.Permissions.Attributes;
using Miki.Framework.Commands.Stages;
using Miki.Framework.Commands.States;
using Miki.Framework.Events;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miki.Modules
{
	[Module("Admin")]
	public class AdminModule
	{
		[Command("ban")]
        [RequiresPermission(PermissionLevel.MODERATOR)]
        public async Task BanAsync(IContext e)
		{
			IDiscordGuildUser currentUser = await e.GetGuild().GetSelfAsync();
			if ((await (e.GetChannel() as IDiscordGuildChannel).GetPermissionsAsync(currentUser)).HasFlag(GuildPermission.BanMembers))
			{
				e.GetArgumentPack().Take(out string userName);
				if (userName == null)
				{
					return;
				}

				IDiscordGuildUser user = await DiscordExtensions.GetUserAsync(userName, e.GetGuild());

				if (user == null)
				{
                    await e.ErrorEmbed(e.GetLocale().GetString("ban_error_user_null"))
						.ToEmbed().QueueAsync(e.GetChannel());
					return;
				}

				IDiscordGuildUser author = await e.GetGuild()
                    .GetMemberAsync(e.GetAuthor().Id);

				if (await user.GetHierarchyAsync() >= await author.GetHierarchyAsync())
				{
                    await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "ban")).ToEmbed()
						.QueueAsync(e.GetChannel());
					return;
				}

				if (await user.GetHierarchyAsync() >= await currentUser.GetHierarchyAsync())
				{
                    await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "ban")).ToEmbed()
						.QueueAsync(e.GetChannel());
					return;
				}

                int prune = 1;
                if(e.GetArgumentPack().Take(out int pruneDays))
                {
                    prune = pruneDays;
                }

                string reason = e.GetArgumentPack().Pack.TakeAll();

				EmbedBuilder embed = new EmbedBuilder
				{
					Title = "🛑 BAN",
					Description = e.GetLocale().GetString("ban_header", $"**{e.GetGuild().Name}**")
				};

				if (!string.IsNullOrWhiteSpace(reason))
				{
					embed.AddInlineField($"💬 {e.GetLocale().GetString("miki_module_admin_kick_reason")}", reason);
				}

				embed.AddInlineField(
                    $"💁 {e.GetLocale().GetString("miki_module_admin_kick_by")}", 
                    $"{e.GetAuthor().Username}#{e.GetAuthor().Discriminator}");

				await embed.ToEmbed().SendToUser(user);

				await e.GetGuild().AddBanAsync(user, prune, reason);
			}
			else
			{
                await e.ErrorEmbed(e.GetLocale().GetString("permission_needed_error", $"`{e.GetLocale().GetString("permission_ban_members")}`"))
					.ToEmbed().QueueAsync(e.GetChannel());
			}
		}

		[Command("clean")]
        [RequiresPermission(PermissionLevel.MODERATOR)]
        public async Task CleanAsync(IContext e)
		{
			await PruneAsync(e, (await e.GetGuild().GetSelfAsync()).Id, null);
		}

		[Command("kick")]
        [RequiresPermission(PermissionLevel.MODERATOR)]
        public async Task KickAsync(IContext e)
		{
			IDiscordGuildUser currentUser = await e.GetGuild().GetSelfAsync();
            
			if ((await (e.GetChannel() as IDiscordGuildChannel).GetPermissionsAsync(currentUser)).HasFlag(GuildPermission.KickMembers))
			{
				IDiscordGuildUser bannedUser;
				IDiscordGuildUser author = await e.GetGuild().GetMemberAsync(e.GetAuthor().Id);

                e.GetArgumentPack().Take(out string userName);

                bannedUser = await DiscordExtensions.GetUserAsync(userName, e.GetGuild());

				if (bannedUser == null)
				{
                    await e.ErrorEmbed(e.GetLocale().GetString("ban_error_user_null"))
						.ToEmbed().QueueAsync(e.GetChannel());
					return;
				}

				if (await bannedUser.GetHierarchyAsync() >= await author.GetHierarchyAsync())
				{
                    await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "kick")).ToEmbed()
						.QueueAsync(e.GetChannel());
					return;
				}

				if (await bannedUser.GetHierarchyAsync() >= await currentUser.GetHierarchyAsync())
				{
                    await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "kick")).ToEmbed()
						.QueueAsync(e.GetChannel());
					return;
				}

				string reason = "";
				if (e.GetArgumentPack().CanTake)
				{
                    reason = e.GetArgumentPack().Pack.TakeAll();
				}

				EmbedBuilder embed = new EmbedBuilder();
				embed.Title = e.GetLocale().GetString("miki_module_admin_kick_header");
				embed.Description = e.GetLocale().GetString("miki_module_admin_kick_description", new object[] { e.GetGuild().Name });

				if (!string.IsNullOrWhiteSpace(reason))
				{
					embed.AddField(e.GetLocale().GetString("miki_module_admin_kick_reason"), reason, true);
				}

				embed.AddField(
                    e.GetLocale().GetString("miki_module_admin_kick_by"),
                    $"{author.Username}#{author.Discriminator}", true);

                embed.Color = new Color(1, 1, 0);

				await embed.ToEmbed().SendToUser(bannedUser);
				await bannedUser.KickAsync(reason);
			}
			else
			{
                await e.ErrorEmbed(e.GetLocale().GetString("permission_needed_error", $"`{e.GetLocale().GetString("permission_kick_members")}`"))
					.ToEmbed().QueueAsync(e.GetChannel());
			}
		}

		[Command("prune")]
        [RequiresPermission(PermissionLevel.MODERATOR)]
		public async Task PruneAsync(IContext e)
		{
			await PruneAsync(e, 0, null);
		}

        public async Task PruneAsync(IContext e, ulong target = 0, string filter = null)
		{
			IDiscordGuildUser invoker = await e.GetGuild()
                .GetSelfAsync();
			if (!(await (e.GetChannel() as IDiscordGuildChannel).GetPermissionsAsync(invoker)).HasFlag(GuildPermission.ManageMessages))
			{
				e.GetChannel()
                    .QueueMessage(e.GetLocale().GetString("miki_module_admin_prune_error_no_access"));
				return;
			}

            if (e.GetArgumentPack().Pack.Length < 1)
            {
                await new EmbedBuilder()
                    .SetTitle("♻ Prune")
                    .SetColor(119, 178, 85)
                    .SetDescription(e.GetLocale().GetString("miki_module_admin_prune_no_arg"))
                    .ToEmbed()
                    .QueueAsync(e.GetChannel());
                return;
            }


            string args = e.GetArgumentPack().Pack.TakeAll();
            string[] argsSplit = args.Split(' ');
            target = e.GetMessage().MentionedUserIds.Count > 0 
                ? (await e.GetGuild().GetMemberAsync(e.GetMessage().MentionedUserIds.First())).Id 
                : target;

            if (int.TryParse(argsSplit[0], out int amount))
			{
				if (amount < 0)
				{
                    await Utils.ErrorEmbed(e, e.GetLocale().GetString("miki_module_admin_prune_error_negative"))
                        .ToEmbed().QueueAsync(e.GetChannel());
                    return;
                }
                if (amount > 100)
                {
                    await Utils.ErrorEmbed(e, e.GetLocale().GetString("miki_module_admin_prune_error_max"))
                        .ToEmbed().QueueAsync(e.GetChannel());
                    return;
                }
            }
            else
            {
                await Utils.ErrorEmbed(e, e.GetLocale().GetString("miki_module_admin_prune_error_parse"))
                    .ToEmbed().QueueAsync(e.GetChannel());
                return;
            }

            if (Regex.IsMatch(e.GetArgumentPack().Pack.TakeAll(), "\"(.*?)\""))
            {
                Regex regex = new Regex("\"(.*?)\"");
                filter = regex.Match(e.GetArgumentPack().Pack.TakeAll()).ToString().Trim('"', ' ');
            }
            
			await e.GetMessage()
                .DeleteAsync(); // Delete the calling message before we get the message history.

			IEnumerable<IDiscordMessage> messages = await e.GetChannel()
                .GetMessagesAsync(amount);
			List<IDiscordMessage> deleteMessages = new List<IDiscordMessage>();

			amount = messages.Count();

			if (amount < 1)
			{
				await e.GetMessage()
                    .DeleteAsync();

                await e.ErrorEmbed(e.GetLocale().GetString("miki_module_admin_prune_no_messages", ">"))
					.ToEmbed().QueueAsync(e.GetChannel());
				return;
			}
			for (int i = 0; i < amount; i++)
			{
				if (target != 0 && messages.ElementAt(i)?.Author.Id != target)
					continue;

                if (filter != null && messages.ElementAt(i)?.Content.IndexOf(filter) < 0)
                    continue;
            
				if (messages.ElementAt(i).Timestamp.AddDays(14) > DateTime.Now)
				{
					deleteMessages.Add(messages.ElementAt(i));
				}
			}

			if (deleteMessages.Count > 0)
			{
				await e.GetChannel()
                    .DeleteMessagesAsync(deleteMessages.ToArray());
			}

			string[] titles = new string[]
			{
				"POW!",
				"BANG!",
				"BAM!",
				"KAPOW!",
				"BOOM!",
				"ZIP!",
				"ZING!",
				"SWOOSH!",
				"POP!"
			};

		    (await new EmbedBuilder
			{
				Title = titles[MikiRandom.Next(titles.Length - 1)],
				Description = e.GetLocale().GetString("miki_module_admin_prune_success", deleteMessages.Count),
				Color = new Color(1, 1, 0.5f)
			}.ToEmbed().QueueAsync(e.GetChannel()))
				.ThenWait(5000)
				.ThenDelete();
		}

        [Command("setevent", "setcommand")]
        [RequiresPermission(PermissionLevel.ADMIN)]
        [RequiresPipelineStage(typeof(StatePipelineStage))]
        public async Task SetCommandAsync(IContext e)
        {
            if (!e.GetArgumentPack().Take(out string commandId))
            {
                // require command argument
                return;
            }
            
            commandId = commandId.Replace('.', ' ');

            var handler = e.GetStage<CommandHandlerStage>();

            var command = handler.GetCommand(commandId);
            if (command == null)
            {
                await e.ErrorEmbed($"'{commandId}' is not a valid command")
                    .ToEmbed().QueueAsync(e.GetChannel());
                return;
            }

            if (!e.GetArgumentPack().Take(out bool setValue))
            {
                return;
            }

            string localeState = (setValue) 
                ? e.GetLocale().GetString("miki_generic_enabled") 
                : e.GetLocale().GetString("miki_generic_disabled");

            bool global = false;

            var context = e.GetService<MikiDbContext>();
            var states = e.GetStage<StatePipelineStage>();

            if (e.GetArgumentPack().Peek(out string g))
            {
                if (g == "-g")
                {
                    global = true;
                    var channels = await e.GetGuild().GetChannelsAsync();
                    foreach (var c in channels)
                    {
                        await states.SetCommandStateAsync(
                            context, 
                            (long)c.Id,
                            command.ToString(),
                            setValue);
                    }
                }
            }
            else
            {
                await states.SetCommandStateAsync(
                    context,
                    (long)e.GetChannel().Id,
                    command.ToString(),
                    setValue);
            }

            await context.SaveChangesAsync();

            string outputDesc = localeState + " " + commandId;
            if (global)
            {
                outputDesc += " in every channel";
            }

            await Utils.SuccessEmbed(e, outputDesc)
                .QueueAsync(e.GetChannel());
        }

		[Command("softban")]
        [RequiresPermission(PermissionLevel.MODERATOR)]
        public async Task SoftbanAsync(IContext e)
		{
			IDiscordGuildUser currentUser = await e.GetGuild().GetSelfAsync();
			if ((await (e.GetChannel() as IDiscordGuildChannel).GetPermissionsAsync(currentUser)).HasFlag(GuildPermission.BanMembers))
			{
				if (!e.GetArgumentPack().Take(out string argObject))
				{
					return;
				}

				IDiscordGuildUser user = await DiscordExtensions.GetUserAsync(argObject, e.GetGuild());
				if (user == null)
				{
                    await e.ErrorEmbed(e.GetLocale().GetString("ban_error_user_null"))
						.ToEmbed().QueueAsync(e.GetChannel());
					return;
				}

                string reason = null;
                if (e.GetArgumentPack().CanTake)
                {
                    reason = e.GetArgumentPack()
                        .Pack.TakeAll();
                }

                IDiscordGuildUser author = await e.GetGuild().GetMemberAsync(e.GetAuthor().Id);

				if (await user.GetHierarchyAsync() >= await author.GetHierarchyAsync())
				{
                    await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "softban")).ToEmbed()
						.QueueAsync(e.GetChannel());
					return;
				}

				if (await user.GetHierarchyAsync() >= await currentUser.GetHierarchyAsync())
				{
                    await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "softban")).ToEmbed()
						.QueueAsync(e.GetChannel());
					return;
				}

				EmbedBuilder embed = new EmbedBuilder
				{
					Title = "⚠ SOFTBAN",
					Description = $"You've been banned from **{e.GetGuild().Name}**!"
				};

				if (!string.IsNullOrWhiteSpace(reason))
				{
					embed.AddInlineField("💬 Reason", reason);
				}

				embed.AddInlineField(
                    "💁 Banned by", 
                    $"{author.Username}#{author.Discriminator}");

				await embed.ToEmbed().SendToUser(user);

				await e.GetGuild().AddBanAsync(user, 1, reason);
				await e.GetGuild().RemoveBanAsync(user);
			}
			else
			{
                await e.ErrorEmbed(e.GetLocale().GetString(
                    "permission_needed_error", 
                    $"`{e.GetLocale().GetString("permission_ban_members")}`"))
					.ToEmbed().QueueAsync(e.GetChannel());
			}
		}
	}
}
