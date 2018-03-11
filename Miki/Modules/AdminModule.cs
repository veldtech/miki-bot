using Discord;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Miki.Common.Events;
using Miki.Common.Extensions;
using Miki.Common.Interfaces;
using Miki.Languages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module(Name = "Admin", CanBeDisabled = false)]
    public class AdminModule
    {
        [Command(Name = "ban", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task BanAsync(EventContext e)
        {
			IDiscordUser currentUser = await e.GetCurrentUserAsync();
			if (currentUser.HasPermissions(e.Channel, DiscordGuildPermission.BanMembers))
			{
				var argObject = e.Arguments.FirstOrDefault();

				if(argObject == null)
				{
					return;
				}

				IDiscordUser user = await argObject.GetUserAsync(e.Guild);
				argObject?.Next();

				if (user == null)
				{
					e.ErrorEmbed(e.GetResource("ban_error_user_null"))
						.QueueToChannel(e.Channel);
					return;
				}

                if(user.Hierarchy >= currentUser.Hierarchy)
                {
                    e.ErrorEmbed(e.GetResource("permission_error_low", "ban"))
                        .QueueToChannel(e.Channel);
                    return;
                }

                if(user.Hierarchy >= e.Author.Hierarchy)
                {
                    e.ErrorEmbed(e.GetResource("permission_user_error_low", "ban"))
                        .QueueToChannel(e.Channel);
                    return;
                }

				int pruneDays = 1;

				if(argObject.AsInt(-1) != -1)
				{
					pruneDays = argObject.AsInt();
					argObject?.Next();
				}

				string reason = argObject.TakeUntilEnd().Argument;

                IDiscordEmbed embed = Utils.Embed;
                embed.Title = "🛑 BAN";
                embed.Description = e.GetResource("ban_header", $"**{e.Guild.Name}**");

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    embed.AddInlineField($"💬 {e.GetResource("miki_module_admin_kick_reason")}", reason);
                }

                embed.AddInlineField($"💁 {e.GetResource("miki_module_admin_kick_by")}", e.Author.Username + "#" + e.Author.Discriminator);

				await embed.SendToUser(user);

                await user.Ban(e.Guild, pruneDays, reason);
            }
            else
            {
                e.ErrorEmbed(e.GetResource("permission_needed_error", $"`{e.GetResource("permission_ban_members")}`"))
                    .QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "softban", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SoftbanAsync(EventContext e)
		{
			IDiscordUser currentUser = await e.GetCurrentUserAsync();
			if (currentUser.HasPermissions(e.Channel, DiscordGuildPermission.BanMembers))
            {
				var argObject = e.Arguments.FirstOrDefault();

				if (argObject == null)
				{
					return;
				}

				IDiscordUser user = await argObject.GetUserAsync(e.Guild);
				argObject?.Next();

				if (user == null)
                {
                    e.ErrorEmbed(e.GetResource("ban_error_user_null"))
                        .QueueToChannel(e.Channel);
                    return;
                }

                if (user.Hierarchy >= currentUser.Hierarchy)
                {
                    e.ErrorEmbed(e.GetResource("permission_error_low", "softban"))
                        .QueueToChannel(e.Channel);
                    return;
                }

                if(user.Hierarchy >= e.Author.Hierarchy)
                {
                    e.ErrorEmbed(e.GetResource("permission_user_error_low", "ban"))
                        .QueueToChannel(e.Channel);
                    return;
                }

				string reason = argObject.TakeUntilEnd().Argument;

                IDiscordEmbed embed = Utils.Embed;
                embed.Title = "⚠ SOFTBAN";
                embed.Description = $"You've been banned from **{e.Guild.Name}**!";

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    embed.AddInlineField("💬 Reason", reason);
                }

                embed.AddInlineField("💁 Banned by", e.Author.Username + "#" + e.Author.Discriminator);

                await embed.SendToUser(user);
                await user.Ban(e.Guild, 1, reason);
                await user.Unban(e.Guild);
            }
            else
            {
                e.ErrorEmbed(e.GetResource("permission_needed_error", $"`{e.GetResource("permission_ban_members")}`"))
                    .QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "clean", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task CleanAsync(EventContext e)
        {
			// TODO: refactor
            await PruneAsync(e, _target: Bot.Instance.CurrentUser.Id);
		}

        [Command(Name = "setevent", Accessibility = EventAccessibility.ADMINONLY, Aliases = new string[] { "setcommand" }, CanBeDisabled = false)]
        public async Task SetCommandAsync(EventContext e)
        {
			Locale locale = new Locale(e.Channel.Id);

			ArgObject arg = e.Arguments.FirstOrDefault();

			if(arg == null)
			{
				return;
			}

			string commandId = arg.Argument;

			IEvent command = e.EventSystem.CommandHandler.GetCommandEvent(commandId);

			if (command == null)
            {
                e.ErrorEmbed($"{commandId} is not a valid command")
					.QueueToChannel(e.Channel);
                return;
            }

			arg = arg.Next();

			bool setValue = arg.AsBoolean() ?? false;

			if (!command.CanBeDisabled)
            {
                e.ErrorEmbed(locale.GetString("miki_admin_cannot_disable", $"`{commandId}`"))
					.QueueToChannel(e.Channel);
                return;
            }

			arg = arg?.Next();

			if (arg != null)
			{
				if (arg.Argument == "-s")
				{
					// TODO: serverwide disable/enable
				}
			}

			await command.SetEnabled(e.Channel.Id, setValue);
            Utils.SuccessEmbed(locale, ((setValue) ? locale.GetString("miki_generic_enabled") : locale.GetString("miki_generic_disabled")) + $" {command.Name}")
				.QueueToChannel(e.Channel);
        }

        [Command(Name = "setmodule", Accessibility = EventAccessibility.ADMINONLY, CanBeDisabled = false)]
        public async Task SetModuleAsync(EventContext e)
        {
			Locale locale = new Locale(e.Channel.Id);

			ArgObject arg = e.Arguments.FirstOrDefault();

			if(arg == null)
			{
				return;
			}

			string moduleName = arg.Argument;

			IModule m = EventSystem.Instance.GetModuleByName(moduleName);
            if (m == null)
            {
                e.ErrorEmbed($"{arg.Argument} is not a valid module.")
					.QueueToChannel(e.Channel);
                return;
            }

			arg = arg.Next();

			bool setValue = arg.AsBoolean() ?? false;

            if (!m.CanBeDisabled && !setValue)
            {
                e.ErrorEmbed(locale.GetString("miki_admin_cannot_disable", $"`{moduleName}`"))
					.QueueToChannel(e.Channel);
                return;
            }

			arg = arg?.Next();

			if(arg != null)
			{
				if(arg.Argument == "-s")
				{
					// TODO: serverwide disable/enable
				}
			}

			await m.SetEnabled(e.Channel.Id, setValue);

            Utils.SuccessEmbed(locale, (setValue ? locale.GetString("miki_generic_enabled") : locale.GetString("miki_generic_disabled")) + $" {m.Name}")
				.QueueToChannel(e.Channel);
        }

        [Command(Name = "kick", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task KickAsync(EventContext e)
		{
			IDiscordUser currentUser = await e.GetCurrentUserAsync();
			ArgObject arg = e.Arguments.FirstOrDefault();

			if (currentUser.HasPermissions(e.Channel, DiscordGuildPermission.KickMembers))
            {
                IDiscordUser bannedUser = null;
				Locale locale = new Locale(e.Channel.Id);

	            bannedUser = await arg?.GetUserAsync(e.Guild) ?? null;

                if (bannedUser == null)
                {
                    e.ErrorEmbed(e.GetResource("ban_error_user_null"))
                        .QueueToChannel(e.Channel);
                    return;
                }

                if (bannedUser.Hierarchy >= currentUser.Hierarchy)
                {
                    e.ErrorEmbed(e.GetResource("permission_error_low", "kick"))
                        .QueueToChannel(e.Channel);
                    return;
                }

                if(bannedUser.Hierarchy >= e.Author.Hierarchy)
                {
                    e.ErrorEmbed(e.GetResource("permission_user_error_low", "ban"))
                        .QueueToChannel(e.Channel);
                    return;
                }

				arg = arg.Next();

                string reason = arg.TakeUntilEnd().Argument;

                IDiscordEmbed embed = Utils.Embed;
                embed.Title = locale.GetString("miki_module_admin_kick_header");
                embed.Description = locale.GetString("miki_module_admin_kick_description", new object[] { e.Guild.Name });

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    embed.AddInlineField(locale.GetString("miki_module_admin_kick_reason"), reason);
                }

                embed.AddInlineField(locale.GetString("miki_module_admin_kick_by"), e.Author.Username + "#" + e.Author.Discriminator);

                embed.Color = new Miki.Common.Color(1, 1, 0);

                await embed.SendToUser(bannedUser);
                await bannedUser.Kick(reason);
            }
            else
            {
                e.ErrorEmbed(e.GetResource("permission_needed_error", $"`{e.GetResource("permission_kick_members")}`"))
                    .QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "prune", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task PruneAsync( EventContext e )
        {
			await PruneAsync(e, 100, 0);
		}

		public async Task PruneAsync(EventContext e, int _amount = 100, ulong _target = 0)
		{
			Locale locale = new Locale(e.Channel.Id);

			IDiscordUser invoker = await e.Guild.GetCurrentUserAsync();
			if (!invoker.HasPermissions(e.Channel, DiscordGuildPermission.ManageMessages))
			{
				e.Channel.QueueMessageAsync(locale.GetString("miki_module_admin_prune_error_no_access"));
				return;
			}

			int amount = _amount;
			string[] argsSplit = e.Arguments.ToString().Split(' ');
			ulong target = e.message.MentionedUserIds.Count > 0 ? (await e.Guild.GetUserAsync(e.message.MentionedUserIds.First())).Id : _target;

			if (!string.IsNullOrEmpty(argsSplit[0]))
			{
				if (int.TryParse(argsSplit[0], out amount))
				{
					if (amount < 0)
					{
						Utils.ErrorEmbed(e, locale.GetString("miki_module_admin_prune_error_negative"))
							.QueueToChannel(e.Channel);
						return;
					}
					if (amount > 100)
					{
						Utils.ErrorEmbed(e, locale.GetString("miki_module_admin_prune_error_max"))
							.QueueToChannel(e.Channel);
						return;
					}
				}
				else
				{
					Utils.ErrorEmbed(e, locale.GetString("miki_module_admin_prune_error_parse"))
						.QueueToChannel(e.Channel);
					return;
				}
			}

			await e.message.DeleteAsync(); // Delete the calling message before we get the message history.

			List<IDiscordMessage> messages = await e.Channel.GetMessagesAsync(amount);
			List<IDiscordMessage> deleteMessages = new List<IDiscordMessage>();

			if (messages.Count < amount)
			{
				amount = messages.Count; // Checks if the amount of messages to delete is more than the amount of messages availiable.
			}

			if (amount <= 1)
			{
				string prefix = await PrefixInstance.Default.GetForGuildAsync(e.Guild.Id);
				await e.message.DeleteAsync();

				e.ErrorEmbed(locale.GetString("miki_module_admin_prune_no_messages", prefix))
					.QueueToChannel(e.Channel);
				return;
			}

			for (int i = 0; i < amount; i++)
			{
				if (target != 0 && messages[i]?.Author.Id != target)
					continue;

				if (messages[i].Timestamp.AddDays(14) > DateTime.Now)
				{
					deleteMessages.Add(messages[i]);
				}
			}

			if (deleteMessages.Count > 0)
			{
				await e.Channel.DeleteMessagesAsync(deleteMessages);
			}

			Task.WaitAll();

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

			IDiscordEmbed embed = Utils.Embed;
			embed.Title = titles[MikiRandom.Next(titles.Length - 1)];
			embed.Description = e.GetResource("miki_module_admin_prune_success", deleteMessages.Count);

			embed.Color = new Miki.Common.Color(1, 1, 0.5f);

			IDiscordMessage _dMessage = await embed.SendToChannel(e.Channel);

			await Task.Delay(5000);

			await _dMessage.DeleteAsync();
		}
	}
}
