using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
				e.Arguments.Take(out string userName);
				if (userName == null)
				{
					return;
				}

				IDiscordGuildUser user = await DiscordExtensions.GetUserAsync(userName, e.Guild);

				if (user == null)
				{
                    await e.ErrorEmbed(e.Locale.GetString("ban_error_user_null"))
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

				IDiscordGuildUser author = await e.Guild.GetMemberAsync(e.Author.Id);

				if (await user.GetHierarchyAsync() >= await author.GetHierarchyAsync())
				{
                    await e.ErrorEmbed(e.Locale.GetString("permission_error_low", "ban")).ToEmbed()
						.QueueToChannelAsync(e.Channel);
					return;
				}

				if (await user.GetHierarchyAsync() >= await currentUser.GetHierarchyAsync())
				{
                    await e.ErrorEmbed(e.Locale.GetString("permission_error_low", "ban")).ToEmbed()
						.QueueToChannelAsync(e.Channel);
					return;
				}

                e.Arguments.Take(out int pruneDays);

                string reason = e.Arguments.Pack.TakeAll();

				EmbedBuilder embed = new EmbedBuilder
				{
					Title = "🛑 BAN",
					Description = e.Locale.GetString("ban_header", $"**{e.Guild.Name}**")
				};

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
                await e.ErrorEmbed(e.Locale.GetString("permission_needed_error", $"`{e.Locale.GetString("permission_ban_members")}`"))
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "clean", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task CleanAsync(EventContext e)
		{
			await PruneAsync(e, 100, (await e.Guild.GetSelfAsync()).Id, null);
		}

		[Command(Name = "kick", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task KickAsync(EventContext e)
		{
			IDiscordGuildUser currentUser = await e.Guild.GetSelfAsync();
            
			if ((await (e.Channel as IDiscordGuildChannel).GetPermissionsAsync(currentUser)).HasFlag(GuildPermission.KickMembers))
			{
				IDiscordGuildUser bannedUser = null;
				IDiscordGuildUser author = await e.Guild.GetMemberAsync(e.Author.Id);

                e.Arguments.Take(out string userName);

                bannedUser = await DiscordExtensions.GetUserAsync(userName, e.Guild);

				if (bannedUser == null)
				{
                    await e.ErrorEmbed(e.Locale.GetString("ban_error_user_null"))
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

				if (await bannedUser.GetHierarchyAsync() >= await author.GetHierarchyAsync())
				{
                    await e.ErrorEmbed(e.Locale.GetString("permission_error_low", "kick")).ToEmbed()
						.QueueToChannelAsync(e.Channel);
					return;
				}

				if (await bannedUser.GetHierarchyAsync() >= await currentUser.GetHierarchyAsync())
				{
                    await e.ErrorEmbed(e.Locale.GetString("permission_error_low", "kick")).ToEmbed()
						.QueueToChannelAsync(e.Channel);
					return;
				}

				string reason = "";
				if (e.Arguments.CanTake)
				{
                    reason = e.Arguments.Pack.TakeAll();
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
                await e.ErrorEmbed(e.Locale.GetString("permission_needed_error", $"`{e.Locale.GetString("permission_kick_members")}`"))
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "prune", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task PruneAsync(EventContext e)
		{
			await PruneAsync(e, 100, 0, null);
		}
		public async Task PruneAsync(EventContext e, int _amount = 100, ulong _target = 0, string _filter = null)
		{
			IDiscordGuildUser invoker = await e.Guild.GetSelfAsync();
			if (!(await (e.Channel as IDiscordGuildChannel).GetPermissionsAsync(invoker)).HasFlag(GuildPermission.ManageMessages))
			{
				e.Channel.QueueMessage(e.Locale.GetString("miki_module_admin_prune_error_no_access"));
				return;
			}

            if (e.Arguments.Pack.Length < 1)
            {
                await new EmbedBuilder()
                    .SetTitle("♻ Prune")
                    .SetColor(119, 178, 85)
                    .SetDescription(e.Locale.GetString("miki_module_admin_prune_no_arg"))
                    .ToEmbed()
                    .QueueToChannelAsync(e.Channel);
                return;
            }

            int amount = _amount;
            string filter = _filter;
            string args = e.Arguments.Pack.TakeAll();
            string[] argsSplit = args.Split(' ');
            ulong target = e.message.MentionedUserIds.Count > 0 ? (await e.Guild.GetMemberAsync(e.message.MentionedUserIds.First())).Id : _target;

            if (int.TryParse(argsSplit[0], out amount))
			{
				if (amount < 0)
				{
                    await Utils.ErrorEmbed(e, e.Locale.GetString("miki_module_admin_prune_error_negative"))
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}
				if (amount > 100)
				{
                    await Utils.ErrorEmbed(e, e.Locale.GetString("miki_module_admin_prune_error_max"))
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}
			}
			else
			{
                await Utils.ErrorEmbed(e, e.Locale.GetString("miki_module_admin_prune_error_parse"))
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

            if (Regex.IsMatch(args, "\"(.*?)\""))
            {
                Regex regex = new Regex("\"(.*?)\"");
                filter = regex.Match(args).ToString().Trim('"', ' ');
            }
            
			await e.message.DeleteAsync(); // Delete the calling message before we get the message history.

			IEnumerable<IDiscordMessage> messages = await e.Channel.GetMessagesAsync(amount);
			List<IDiscordMessage> deleteMessages = new List<IDiscordMessage>();

			amount = messages.Count();

			if (amount < 1)
			{
				await e.message.DeleteAsync();

                await e.ErrorEmbed(e.Locale.GetString("miki_module_admin_prune_no_messages", ">"))
					.ToEmbed().QueueToChannelAsync(e.Channel);
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
				await e.Channel.DeleteMessagesAsync(deleteMessages.ToArray());
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
				Description = e.Locale.GetString("miki_module_admin_prune_success", deleteMessages.Count),
				Color = new Color(1, 1, 0.5f)
			}.ToEmbed().QueueToChannelAsync(e.Channel))
				.ThenWait(5000)
				.ThenDelete();
		}

		[Command(Name = "setevent", 
			Accessibility = EventAccessibility.ADMINONLY, 
			Aliases = new string[] { "setcommand" }, 
			CanBeDisabled = false)]
		public async Task SetCommandAsync(EventContext e)
		{
			if (!e.Arguments.Take(out string commandId))
			{
				return;
			}

			Event command = e.EventSystem.GetCommandHandler<SimpleCommandHandler>().GetCommandById(commandId);

			if (command == null)
			{
                await e.ErrorEmbed($"{commandId} is not a valid command")
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

			if (!command.CanBeDisabled)
			{
                await e.ErrorEmbed(e.Locale.GetString("miki_admin_cannot_disable", $"`{commandId}`"))
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

			if(!e.Arguments.Take(out bool setValue))
			{
				return;
			}

			string localeState = (setValue) ? e.Locale.GetString("miki_generic_enabled") : e.Locale.GetString("miki_generic_disabled");

			bool global = false;

			using (var context = new MikiContext())
			{
                var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));

                if (e.Arguments.Peek(out string g))
				{
                    if (g == "-g")
                    {
                        global = true;
                        var channels = await e.Guild.GetChannelsAsync();
                        foreach (var c in channels)
                        {
                            await command.SetEnabled(context, cache, c.Id, setValue);
                        }
                    }
				}
				else
				{
					await command.SetEnabled(context, cache, e.Channel.Id, setValue);
				}

				await context.SaveChangesAsync();
			}

			string outputDesc = localeState + " " + commandId;

			if (global)
			{
				outputDesc += " in every channel.";
			}
			else
			{
				outputDesc += ".";
			}

			await Utils.SuccessEmbed(e, outputDesc)
				.QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "setmodule", Accessibility = EventAccessibility.ADMINONLY, CanBeDisabled = false)]
		public async Task SetModuleAsync(EventContext e)
		{
            var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));

			if (e.Arguments.Take(out string moduleName))
			{
				return;
			}

			Module m = e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Modules.FirstOrDefault(x => x.Name == moduleName);

			if (m == null)
			{
                await e.ErrorEmbed($"{moduleName} is not a valid module.")
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

            if (e.Arguments.Take(out bool setValue))
            {
                if (!m.CanBeDisabled && !setValue)
                {
                    await e.ErrorEmbed(e.Locale.GetString("miki_admin_cannot_disable", $"`{moduleName}`"))
                        .ToEmbed().QueueToChannelAsync(e.Channel);
                    return;
                }
            }

			await m.SetEnabled(cache, e.Guild.Id, setValue);

            await e.SuccessEmbed((setValue ? e.Locale.GetString("miki_generic_enabled") : e.Locale.GetString("miki_generic_disabled")) + $" {m.Name}")
				.QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "softban", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task SoftbanAsync(EventContext e)
		{
			IDiscordGuildUser currentUser = await e.Guild.GetSelfAsync();
			if ((await (e.Channel as IDiscordGuildChannel).GetPermissionsAsync(currentUser)).HasFlag(GuildPermission.BanMembers))
			{
				if (!e.Arguments.Take(out string argObject))
				{
					return;
				}

				IDiscordGuildUser user = await DiscordExtensions.GetUserAsync(argObject, e.Guild);

				string reason = null;

				if (e.Arguments.CanTake)
				{
                    reason = e.Arguments.Pack.TakeAll();
				}

				if (user == null)
				{
                    await e.ErrorEmbed(e.Locale.GetString("ban_error_user_null"))
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

				IDiscordGuildUser author = await e.Guild.GetMemberAsync(e.Author.Id);

				if (await user.GetHierarchyAsync() >= await author.GetHierarchyAsync())
				{
                    await e.ErrorEmbed(e.Locale.GetString("permission_error_low", "softban")).ToEmbed()
						.QueueToChannelAsync(e.Channel);
					return;
				}

				if (await user.GetHierarchyAsync() >= await currentUser.GetHierarchyAsync())
				{
                    await e.ErrorEmbed(e.Locale.GetString("permission_error_low", "softban")).ToEmbed()
						.QueueToChannelAsync(e.Channel);
					return;
				}

				EmbedBuilder embed = new EmbedBuilder
				{
					Title = "⚠ SOFTBAN",
					Description = $"You've been banned from **{e.Guild.Name}**!"
				};

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
                await e.ErrorEmbed(e.Locale.GetString("permission_needed_error", $"`{e.Locale.GetString("permission_ban_members")}`"))
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}
	}
}
