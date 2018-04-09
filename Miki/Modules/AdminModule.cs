using Discord;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Miki.Languages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Miki.Framework.Extension;
using Discord.WebSocket;

namespace Miki.Modules
{
	[Module(Name = "Admin", CanBeDisabled = false)]
	public class AdminModule
	{
		[Command(Name = "ban", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task BanAsync(EventContext e)
		{
			IGuildUser currentUser = await e.Guild.GetCurrentUserAsync();
			if (currentUser.GuildPermissions.Has(GuildPermission.BanMembers))
			{
				var argObject = e.Arguments.FirstOrDefault();

				if (argObject == null)
				{
					return;
				}

				IGuildUser user = await argObject.GetUserAsync(e.Guild);
				argObject?.Next();

				if (user == null)
				{
					e.ErrorEmbed(e.GetResource("ban_error_user_null"))
						.Build().QueueToChannel(e.Channel);
					return;
				}

				if ((user as SocketGuildUser).Hierarchy >= (e.Author as SocketGuildUser).Hierarchy)
				{
					e.ErrorEmbed(e.GetResource("permission_error_low", "ban")).Build()
						.QueueToChannel(e.Channel);
					return;
				}

				if ((user as SocketGuildUser).Hierarchy >= (currentUser as SocketGuildUser).Hierarchy)
				{
					e.ErrorEmbed(e.GetResource("permission_error_low", "ban")).Build()
						.QueueToChannel(e.Channel);
					return;
				}

				int pruneDays = 1;

				if (argObject.AsInt(-1) != -1)
				{
					pruneDays = argObject.AsInt();
					argObject?.Next();
				}

				string reason = argObject.TakeUntilEnd().Argument;

				EmbedBuilder embed = Utils.Embed;
				embed.Title = "🛑 BAN";
				embed.Description = e.GetResource("ban_header", $"**{e.Guild.Name}**");

				if (!string.IsNullOrWhiteSpace(reason))
				{
					embed.AddInlineField($"💬 {e.GetResource("miki_module_admin_kick_reason")}", reason);
				}

				embed.AddInlineField($"💁 {e.GetResource("miki_module_admin_kick_by")}", e.Author.Username + "#" + e.Author.Discriminator);

				await embed.Build().SendToUser(user);

				await user.Guild.AddBanAsync(user, pruneDays, reason);
			}
			else
			{
				e.ErrorEmbed(e.GetResource("permission_needed_error", $"`{e.GetResource("permission_ban_members")}`"))
					.Build().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "clean", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task CleanAsync(EventContext e)
		{
			// TODO: refactor
			await PruneAsync(e, _target: Bot.Instance.Client.CurrentUser.Id);
		}

		[Command(Name = "editexp", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task EditExpAsync(EventContext e)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg == null)
				throw new ArgumentException();

			IUser target = await arg.GetUserAsync(e.Guild);
		}

		[Command(Name = "kick", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task KickAsync(EventContext e)
		{
			IGuildUser currentUser = await e.Guild.GetCurrentUserAsync();
			ArgObject arg = e.Arguments.FirstOrDefault();

			if (currentUser.GuildPermissions.Has(GuildPermission.KickMembers))
			{
				IGuildUser bannedUser = null;
				bannedUser = await arg?.GetUserAsync(e.Guild) ?? null;

				if (bannedUser == null)
				{
					e.ErrorEmbed(e.GetResource("ban_error_user_null"))
						.Build().QueueToChannel(e.Channel);
					return;
				}

				if ((bannedUser as SocketGuildUser).Hierarchy >= (e.Author as SocketGuildUser).Hierarchy)
				{
					e.ErrorEmbed(e.GetResource("permission_error_low", "kick")).Build()
						.QueueToChannel(e.Channel);
					return;
				}

				if ((bannedUser as SocketGuildUser).Hierarchy >= (currentUser as SocketGuildUser).Hierarchy)
				{
					e.ErrorEmbed(e.GetResource("permission_error_low", "kick")).Build()
						.QueueToChannel(e.Channel);
					return;
				}

				arg = arg.Next();

				string reason = arg.TakeUntilEnd().Argument;

				EmbedBuilder embed = new EmbedBuilder();
				embed.Title = e.GetResource("miki_module_admin_kick_header");
				embed.Description = e.GetResource("miki_module_admin_kick_description", new object[] { e.Guild.Name });

				if (!string.IsNullOrWhiteSpace(reason))
				{
					embed.AddInlineField(e.GetResource("miki_module_admin_kick_reason"), reason);
				}

				embed.AddInlineField(e.GetResource("miki_module_admin_kick_by"), e.Author.Username + "#" + e.Author.Discriminator);

				embed.Color = new Color(1, 1, 0);

				await embed.Build().SendToUser(bannedUser);
				await bannedUser.KickAsync(reason);
			}
			else
			{
				e.ErrorEmbed(e.GetResource("permission_needed_error", $"`{e.GetResource("permission_kick_members")}`"))
					.Build().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "prune", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task PruneAsync(EventContext e)
		{
			await PruneAsync(e, 100, 0);
		}
		public async Task PruneAsync(EventContext e, int _amount = 100, ulong _target = 0)
		{
			IGuildUser invoker = await e.Guild.GetCurrentUserAsync();
			if (!invoker.GuildPermissions.Has(GuildPermission.ManageMessages))
			{
				e.Channel.QueueMessageAsync(e.GetResource("miki_module_admin_prune_error_no_access"));
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
						Utils.ErrorEmbed(e, e.GetResource("miki_module_admin_prune_error_negative"))
							.Build().QueueToChannel(e.Channel);
						return;
					}
					if (amount > 100)
					{
						Utils.ErrorEmbed(e, e.GetResource("miki_module_admin_prune_error_max"))
							.Build().QueueToChannel(e.Channel);
						return;
					}
				}
				else
				{
					Utils.ErrorEmbed(e, e.GetResource("miki_module_admin_prune_error_parse"))
						.Build().QueueToChannel(e.Channel);
					return;
				}
			}

			await e.message.DeleteAsync(); // Delete the calling message before we get the message history.

			IEnumerable<IMessage> messages = await e.Channel.GetMessagesAsync(amount).FlattenAsync();
			List<IMessage> deleteMessages = new List<IMessage>();

			amount = messages.Count(); // Checks if the amount of messages to delete is more than the amount of messages availiable.

			if (amount < 1)
			{
				string prefix = await PrefixInstance.Default.GetForGuildAsync(e.Guild.Id);
				await e.message.DeleteAsync();

				e.ErrorEmbed(e.GetResource("miki_module_admin_prune_no_messages", prefix))
					.Build().QueueToChannel(e.Channel);
				return;
			}

			for (int i = 0; i < amount; i++)
			{
				if (target != 0 && messages.ElementAt(i)?.Author.Id != target)
					continue;

				if (messages.ElementAt(i).Timestamp.AddDays(14) > DateTime.Now)
				{
					deleteMessages.Add(messages.ElementAt(i));
				}
			}

			if (deleteMessages.Count > 0)
			{
				await (e.Channel as ITextChannel).DeleteMessagesAsync(deleteMessages);
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

			EmbedBuilder embed = Utils.Embed;
			embed.Title = titles[MikiRandom.Next(titles.Length - 1)];
			embed.Description = e.GetResource("miki_module_admin_prune_success", deleteMessages.Count);

			embed.Color = new Color(1, 1, 0.5f);

			IMessage _dMessage = await embed.Build().SendToChannel(e.Channel);

			Task.Run(async () =>
			{
				await Task.Delay(5000);
				await _dMessage.DeleteAsync();
			});
		}

		[Command(Name = "setevent", Accessibility = EventAccessibility.ADMINONLY, Aliases = new string[] { "setcommand" }, CanBeDisabled = false)]
		public async Task SetCommandAsync(EventContext e)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg == null)
			{
				return;
			}

			string commandId = arg.Argument;

			Event command = e.EventSystem.CommandHandler.GetCommandEvent(commandId);

			if (command == null)
			{
				e.ErrorEmbed($"{commandId} is not a valid command")
					.Build().QueueToChannel(e.Channel);
				return;
			}

			arg = arg.Next();

			bool? setValue = arg.AsBoolean();
			if (!setValue.HasValue)
				setValue = arg.Argument.ToLower() == "yes" || arg.Argument == "1";

			if (!command.CanBeDisabled)
			{
				e.ErrorEmbed(e.GetResource("miki_admin_cannot_disable", $"`{commandId}`"))
					.Build().QueueToChannel(e.Channel);
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

			await command.SetEnabled(e.Channel.Id, setValue ?? false);
			Utils.SuccessEmbed(e.Channel.Id, ((setValue ?? false) ? e.GetResource("miki_generic_enabled") : e.GetResource("miki_generic_disabled")) + $" {command.Name}")
				.QueueToChannel(e.Channel);
		}

		[Command(Name = "setmodule", Accessibility = EventAccessibility.ADMINONLY, CanBeDisabled = false)]
		public async Task SetModuleAsync(EventContext e)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg == null)
			{
				return;
			}

			string moduleName = arg.Argument;

			Module m = EventSystem.Instance.GetModuleByName(moduleName);

			if (m == null)
			{
				e.ErrorEmbed($"{arg.Argument} is not a valid module.")
					.Build().QueueToChannel(e.Channel);
				return;
			}

			arg = arg?.Next();

			bool? setValue = arg.AsBoolean();
			if (!setValue.HasValue)
				setValue = arg.Argument.ToLower() == "yes" || arg.Argument == "1";

			if (!m.CanBeDisabled && !setValue.Value)
			{
				e.ErrorEmbed(e.GetResource("miki_admin_cannot_disable", $"`{moduleName}`"))
					.Build().QueueToChannel(e.Channel);
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

			await m.SetEnabled(e.Channel.Id, (setValue ?? false));

			Utils.SuccessEmbed(e.Channel.Id, ((setValue ?? false) ? e.GetResource("miki_generic_enabled") : e.GetResource("miki_generic_disabled")) + $" {m.Name}")
				.QueueToChannel(e.Channel);
		}

		[Command(Name = "softban", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task SoftbanAsync(EventContext e)
		{
			IGuildUser currentUser = await e.Guild.GetCurrentUserAsync();
			if (currentUser.GuildPermissions.Has(GuildPermission.BanMembers))
			{
				var argObject = e.Arguments.FirstOrDefault();

				if (argObject == null)
				{
					return;
				}

				IGuildUser user = await argObject.GetUserAsync(e.Guild);
				argObject?.Next();

				if (user == null)
				{
					e.ErrorEmbed(e.GetResource("ban_error_user_null"))
						.Build().QueueToChannel(e.Channel);
					return;
				}

				if ((user as SocketGuildUser).Hierarchy >= (e.Author as SocketGuildUser).Hierarchy)
				{
					e.ErrorEmbed(e.GetResource("permission_error_low", "softban")).Build()
						.QueueToChannel(e.Channel);
					return;
				}

				if ((user as SocketGuildUser).Hierarchy >= (currentUser as SocketGuildUser).Hierarchy)
				{
					e.ErrorEmbed(e.GetResource("permission_error_low", "softban")).Build()
						.QueueToChannel(e.Channel);
					return;
				}


				string reason = argObject.TakeUntilEnd().Argument;

				EmbedBuilder embed = Utils.Embed;
				embed.Title = "⚠ SOFTBAN";
				embed.Description = $"You've been banned from **{e.Guild.Name}**!";

				if (!string.IsNullOrWhiteSpace(reason))
				{
					embed.AddInlineField("💬 Reason", reason);
				}

				embed.AddInlineField("💁 Banned by", e.Author.Username + "#" + e.Author.Discriminator);

				await embed.Build().SendToUser(user);
				await user.Guild.AddBanAsync(user, 1, reason);
				await user.Guild.RemoveBanAsync(user);
			}
			else
			{
				e.ErrorEmbed(e.GetResource("permission_needed_error", $"`{e.GetResource("permission_ban_members")}`"))
					.Build().QueueToChannel(e.Channel);
			}
		}
	}
}
