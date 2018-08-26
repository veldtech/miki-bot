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
using Miki.Discord.Common;
using Miki.Discord;
using Miki.Discord.Rest;
using Miki.Discord.Common.Gateway.Packets;
using Miki.Discord.Common.Gateway;

namespace Miki.Modules
{
	[Module(Name = "Admin", CanBeDisabled = false)]
	public class AdminModule
	{
		[Command(Name = "ban", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task BanAsync(EventContext e)
		{
			IDiscordGuildUser currentUser = await e.Guild.GetSelfAsync();
			if ((await (e.Channel as IDiscordGuildChannel).GetPermissionsAsync(currentUser)).HasFlag(GuildPermission.BanMembers))
			{
				var argObject = e.Arguments.FirstOrDefault();

				if (argObject == null)
				{
					return;
				}

				IDiscordGuildUser user = await argObject.GetUserAsync(e.Guild);
				argObject?.Next();

				if (user == null)
				{
					e.ErrorEmbed(e.Locale.GetString("ban_error_user_null"))
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				IDiscordGuildUser author = e.Guild.GetMember(e.Author.Id);

				if (user.Hierarchy >= author.Hierarchy)
				{
					e.ErrorEmbed(e.Locale.GetString("permission_error_low", "ban")).ToEmbed()
						.QueueToChannel(e.Channel);
					return;
				}

				if (user.Hierarchy >= currentUser.Hierarchy)
				{
					e.ErrorEmbed(e.Locale.GetString("permission_error_low", "ban")).ToEmbed()
						.QueueToChannel(e.Channel);
					return;
				}

				int pruneDays = 1;

				if (argObject.AsInt() != null)
				{
					pruneDays = argObject.AsInt().Value;
					argObject?.Next();
				}

				string reason = argObject.TakeUntilEnd().Argument;

				EmbedBuilder embed = Utils.Embed;
				embed.Title = "🛑 BAN";
				embed.Description = e.Locale.GetString("ban_header", $"**{e.Guild.Name}**");

				if (!string.IsNullOrWhiteSpace(reason))
				{
					embed.AddInlineField($"💬 {e.Locale.GetString("miki_module_admin_kick_reason")}", reason);
				}

				embed.AddInlineField($"💁 {e.Locale.GetString("miki_module_admin_kick_by")}", e.Author.Username + "#" + e.Author.Discriminator);

				await embed.ToEmbed().SendToUser(user);

				await e.Guild.AddBanAsync(user, 1, reason);
			}
			else
			{
				e.ErrorEmbed(e.Locale.GetString("permission_needed_error", $"`{e.Locale.GetString("permission_ban_members")}`"))
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "clean", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task CleanAsync(EventContext e)
		{
			await PruneAsync(e, 100, (await e.Guild.GetSelfAsync()).Id);
		}

		[Command(Name = "editexp", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task EditExpAsync(EventContext e)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg == null)
				throw new ArgumentException();

			IDiscordUser target = await arg.GetUserAsync(e.Guild);
		}

		[Command(Name = "kick", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task KickAsync(EventContext e)
		{
			IDiscordGuildUser currentUser = await e.Guild.GetSelfAsync();
			ArgObject arg = e.Arguments.FirstOrDefault();

			if ((await (e.Channel as IDiscordGuildChannel).GetPermissionsAsync(currentUser)).HasFlag(GuildPermission.KickMembers))
			{
				IDiscordGuildUser bannedUser = null;
				IDiscordGuildUser author = e.Guild.GetMember(e.Author.Id);

				bannedUser = await arg?.GetUserAsync(e.Guild) ?? null;

				if (bannedUser == null)
				{
					e.ErrorEmbed(e.Locale.GetString("ban_error_user_null"))
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				if ((bannedUser as IDiscordGuildUser).Hierarchy >= author.Hierarchy)
				{
					e.ErrorEmbed(e.Locale.GetString("permission_error_low", "kick")).ToEmbed()
						.QueueToChannel(e.Channel);
					return;
				}

				if ((bannedUser as IDiscordGuildUser).Hierarchy >= author.Hierarchy)
				{
					e.ErrorEmbed(e.Locale.GetString("permission_error_low", "kick")).ToEmbed()
						.QueueToChannel(e.Channel);
					return;
				}

				string reason = "";

				if (!arg.IsLast)
				{
					arg = arg.Next();

					reason = arg.TakeUntilEnd().Argument;
				}

				EmbedBuilder embed = new EmbedBuilder();
				embed.Title = e.Locale.GetString("miki_module_admin_kick_header");
				embed.Description = e.Locale.GetString("miki_module_admin_kick_description", new object[] { e.Guild.Name });

				if (!string.IsNullOrWhiteSpace(reason))
				{
					embed.AddField(e.Locale.GetString("miki_module_admin_kick_reason"), reason, true);
				}

				embed.AddField(e.Locale.GetString("miki_module_admin_kick_by"), e.Author.Username + "#" + e.Author.Discriminator, true);

				embed.Color = new Color(1, 1, 0);

				await embed.ToEmbed().SendToUser(bannedUser);
				await bannedUser.KickAsync(reason);
			}
			else
			{
				e.ErrorEmbed(e.Locale.GetString("permission_needed_error", $"`{e.Locale.GetString("permission_kick_members")}`"))
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "prune", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task PruneAsync(EventContext e)
		{
			await PruneAsync(e, 100, 0);
		}
		public async Task PruneAsync(EventContext e, int _amount = 100, ulong _target = 0)
		{
			IDiscordGuildUser invoker = await e.Guild.GetSelfAsync();
			if (!(await (e.Channel as IDiscordGuildChannel).GetPermissionsAsync(invoker)).HasFlag(GuildPermission.ManageMessages))
			{
				e.Channel.QueueMessageAsync(e.Locale.GetString("miki_module_admin_prune_error_no_access"));
				return;
			}

			int amount = _amount;
			string[] argsSplit = e.Arguments.ToString().Split(' ');
			ulong target = e.message.MentionedUserIds.Count > 0 ? e.Guild.GetMember(e.message.MentionedUserIds.First()).Id : _target;

			if (!string.IsNullOrEmpty(argsSplit[0]))
			{
				if (int.TryParse(argsSplit[0], out amount))
				{
					if (amount < 0)
					{
						Utils.ErrorEmbed(e, e.Locale.GetString("miki_module_admin_prune_error_negative"))
							.ToEmbed().QueueToChannel(e.Channel);
						return;
					}
					if (amount > 100)
					{
						Utils.ErrorEmbed(e, e.Locale.GetString("miki_module_admin_prune_error_max"))
							.ToEmbed().QueueToChannel(e.Channel);
						return;
					}
				}
				else
				{
					Utils.ErrorEmbed(e, e.Locale.GetString("miki_module_admin_prune_error_parse"))
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}
			}

			await e.message.DeleteAsync(); // Delete the calling message before we get the message history.

			//IEnumerable<IDiscordMessage> messages = await e.Channel.GetMessagesAsync(amount).FlattenAsync();
			List<IDiscordMessage> deleteMessages = new List<IDiscordMessage>();

			//amount = messages.Count(); // Checks if the amount of messages to delete is more than the amount of messages availiable.

			//if (amount < 1)
			//{
			//	string prefix = await e.commandHandler.GetPrefixAsync(e.Guild.Id);
			//	await e.message.DeleteAsync();

			//	e.ErrorEmbed(e.GetResource("miki_module_admin_prune_no_messages", prefix))
			//		.ToEmbed().QueueToChannel(e.Channel);
			//	return;
			//}

			//for (int i = 0; i < amount; i++)
			//{
			//	if (target != 0 && messages.ElementAt(i)?.Author.Id != target)
			//		continue;

			//	if (messages.ElementAt(i).Timestamp.AddDays(14) > DateTime.Now)
			//	{
			//		deleteMessages.Add(messages.ElementAt(i));
			//	}
			//}

			//if (deleteMessages.Count > 0)
			//{
			//	await e.Channel.DeleteMessagesAsync(deleteMessages);
			//}

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
			embed.Description = e.Locale.GetString("miki_module_admin_prune_success", deleteMessages.Count);

			embed.Color = new Color(1, 1, 0.5f);

			IDiscordMessage _dMessage = await embed.ToEmbed().SendToChannel(e.Channel);

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

			Event command = e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Commands.FirstOrDefault(x => x.Name == commandId);

			if (command == null)
			{
				e.ErrorEmbed($"{commandId} is not a valid command")
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			arg = arg.Next();

			bool? setValue = arg.AsBoolean();
			if (!setValue.HasValue)
				setValue = arg.Argument.ToLower() == "yes" || arg.Argument == "1";

			if (!command.CanBeDisabled)
			{
				e.ErrorEmbed(e.Locale.GetString("miki_admin_cannot_disable", $"`{commandId}`"))
					.ToEmbed().QueueToChannel(e.Channel);
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

			await command.SetEnabled(Global.RedisClient, e.Channel.Id, setValue ?? false);
			e.SuccessEmbed(((setValue ?? false) ? e.Locale.GetString("miki_generic_enabled") : e.Locale.GetString("miki_generic_disabled")) + $" {command.Name}")
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

			Module m = e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Modules.FirstOrDefault(x => x.Name == moduleName);


			if (m == null)
			{
				e.ErrorEmbed($"{arg.Argument} is not a valid module.")
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			arg = arg?.Next();

			bool? setValue = arg.AsBoolean();
			if (!setValue.HasValue)
				setValue = arg.Argument.ToLower() == "yes" || arg.Argument == "1";

			if (!m.CanBeDisabled && !setValue.Value)
			{
				e.ErrorEmbed(e.Locale.GetString("miki_admin_cannot_disable", $"`{moduleName}`"))
					.ToEmbed().QueueToChannel(e.Channel);
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

			await m.SetEnabled(Global.RedisClient, e.Channel.Id, (setValue ?? false));

			e.SuccessEmbed(((setValue ?? false) ? e.Locale.GetString("miki_generic_enabled") : e.Locale.GetString("miki_generic_disabled")) + $" {m.Name}")
				.QueueToChannel(e.Channel);
		}

		[Command(Name = "softban", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task SoftbanAsync(EventContext e)
		{
			IDiscordGuildUser currentUser = await e.Guild.GetSelfAsync();
			if ((await (e.Channel as IDiscordGuildChannel).GetPermissionsAsync(currentUser)).HasFlag(GuildPermission.BanMembers))
			{
				var argObject = e.Arguments.FirstOrDefault();

				if (argObject == null)
				{
					return;
				}

				IDiscordGuildUser user = await argObject.GetUserAsync(e.Guild);

				string reason = null;

				if (!argObject.IsLast)
				{
					argObject?.Next();
					reason = argObject.TakeUntilEnd().Argument;
				}

				if (user == null)
				{
					e.ErrorEmbed(e.Locale.GetString("ban_error_user_null"))
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				IDiscordGuildUser author = e.Guild.GetMember(e.Author.Id);

				if (user.Hierarchy >= author.Hierarchy)
				{
					e.ErrorEmbed(e.Locale.GetString("permission_error_low", "softban")).ToEmbed()
						.QueueToChannel(e.Channel);
					return;
				}

				if (user.Hierarchy >= currentUser.Hierarchy)
				{
					e.ErrorEmbed(e.Locale.GetString("permission_error_low", "softban")).ToEmbed()
						.QueueToChannel(e.Channel);
					return;
				}

				EmbedBuilder embed = Utils.Embed;
				embed.Title = "⚠ SOFTBAN";
				embed.Description = $"You've been banned from **{e.Guild.Name}**!";

				if (!string.IsNullOrWhiteSpace(reason))
				{
					embed.AddInlineField("💬 Reason", reason);
				}

				embed.AddInlineField("💁 Banned by", e.Author.Username + "#" + e.Author.Discriminator);

				await embed.ToEmbed().SendToUser(user);

				await e.Guild.AddBanAsync(user, 1, reason);
				await e.Guild.RemoveBanAsync(user);
			}
			else
			{
				e.ErrorEmbed(e.Locale.GetString("permission_needed_error", $"`{e.Locale.GetString("permission_ban_members")}`"))
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}
	}
}
