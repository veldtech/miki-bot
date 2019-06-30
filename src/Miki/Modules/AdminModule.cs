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
using Miki.Framework.Commands.Permissions.Models;
using Miki.Framework.Commands.Stages;
using Miki.Framework.Events;
using Miki.Logging;
using Miki.Models;
using Miki.Utility;
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
        [DefaultPermission(PermissionStatus.Deny)]
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
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task CleanAsync(IContext e)
		{
			await PruneAsync(e, (await e.GetGuild().GetSelfAsync()).Id, null);
		}

		[Command("kick")]
        [DefaultPermission(PermissionStatus.Deny)]
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
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task PruneAsync(IContext e)
		{
			await PruneAsync(e, 0, null);
		}

        public async Task PruneAsync(IContext e, ulong target = 0, string filter = null)
		{
			IDiscordGuildUser invoker = await e.GetGuild()
                .GetSelfAsync();
            var locale = e.GetLocale();

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
                    await Utils.ErrorEmbed(e, locale.GetString("miki_module_admin_prune_error_negative"))
                        .ToEmbed().QueueAsync(e.GetChannel());
                    return;
                }
                if (amount > 100)
                {
                    await Utils.ErrorEmbed(e, locale.GetString("miki_module_admin_prune_error_max"))
                        .ToEmbed().QueueAsync(e.GetChannel());
                    return;
                }
            }
            else
            {
                await Utils.ErrorEmbed(e, locale.GetString("miki_module_admin_prune_error_parse"))
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

                await e.ErrorEmbed(locale.GetString(
                        "miki_module_admin_prune_no_messages", 
                        e.GetPrefixMatch()))
					.ToEmbed()
                    .QueueAsync(e.GetChannel());
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

            await e.SuccessEmbedResource("miki_module_admin_prune_success", deleteMessages.Count)
                .QueueAsync(e.GetChannel())
                .ThenWaitAsync(5000)
                .ThenDeleteAsync();
		}

        [Command("permissions")]
        public class PermissionsCommand
        {
            [Command("set")]
            [RequiresPipelineStage(typeof(PermissionPipelineStage))]
            [DefaultPermission(PermissionStatus.Deny)]
            public async Task SetPermissionsAsync(IContext e)
            {
                var permissions = e.GetStage<PermissionPipelineStage>();

                if (!e.GetArgumentPack().Take(out string permission))
                {
                    return;
                }

                if (!Enum.TryParse<PermissionStatus>(permission, true, out var level))
                {
                    // invalid permission level
                    return;
                }

                if (!e.GetArgumentPack().Take(out string user))
                {
                    return;
                }

                var userObject = await e.GetGuild().FindUserAsync(user);
                if (userObject == null)
                {
                    // User not found
                    return;
                }

                if (!e.GetArgumentPack().Take(out string commandName))
                {
                    return;
                }

                var ownPermission = await permissions.GetAllowedForUser(e, e.GetMessage(), commandName);
                if (!ownPermission)
                {
                    // You can't do that :)
                    return;
                }

                // TODO: implement 
                //await permissions.SetForUserAsync(e, (long)userObject.Id, level);

                await e.SuccessEmbedResource("permission_set", userObject, level)
                    .QueueAsync(e.GetChannel());
            }
        }

        [Command("setevent", "setcommand")]
        [DefaultPermission(PermissionStatus.Deny)]
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

            if (e.GetArgumentPack().Peek(out string g))
            {
                if (g == "-g")
                {
                    global = true;
                    var channels = await e.GetGuild().GetChannelsAsync();
                    foreach (var c in channels)
                    {
                    }
                }
            }
            else
            {
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

                await embed.ToEmbed()
                    .SendToUser(user);

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
