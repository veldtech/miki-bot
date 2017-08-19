using Discord;
using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Extensions;
using IA.SDK.Interfaces;
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
            if (e.Guild.CurrentUser.HasPermissions(e.Channel, DiscordGuildPermission.BanMembers))
            {
                List<string> arg = e.arguments.Split(' ').ToList();
                IDiscordUser bannedUser = null;

                if (e.message.MentionedUserIds.Count > 0)
                {
                    bannedUser = await e.Guild.GetUserAsync(e.message.MentionedUserIds.First());
                    arg.RemoveAll(x => x.Contains(e.message.MentionedUserIds.First().ToString()));
                }
                else
                {
                    if (arg.Count > 0)
                    {
                        bannedUser = await e.Guild.GetUserAsync(ulong.Parse(arg[0]));
                        arg.RemoveAt(0);
                    }
                }

                if (bannedUser == null)
                {
                    await e.ErrorEmbed(e.GetResource("ban_error_user_null"))
                        .SendToChannel(e.Channel);
                    return;
                }

                if(bannedUser.Hierarchy >= e.Guild.CurrentUser.Hierarchy)
                {
                    await e.ErrorEmbed(e.GetResource("permission_error_low"))
                        .SendToChannel(e.Channel);
                    return;
                }

                string reason = string.Join(" ", arg);

                IDiscordEmbed embed = Utils.Embed;
                embed.Title = "🛑 BAN";
                embed.Description = e.GetResource("ban_header", $"**{e.Guild.Name}**");

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    embed.AddInlineField($"💬 {e.GetResource("miki_module_admin_kick_reason")}", reason);
                }

                embed.AddInlineField($"💁 {e.GetResource("miki_module_admin_kick_by")}", e.Author.Username + "#" + e.Author.Discriminator);

                await bannedUser.SendMessage(embed);
                await bannedUser.Ban(e.Guild, 1, reason);
            }
            else
            {
                await e.ErrorEmbed(e.GetResource("permission_needed_error", $"`{e.GetResource("permission_ban_members")}`"))
                    .SendToChannel(e.Channel);
            }
        }

        [Command(Name = "softban", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SoftbanAsync(EventContext e)
        {
            if (e.Guild.CurrentUser.HasPermissions(e.Channel, DiscordGuildPermission.BanMembers))
            {
                List<string> arg = e.arguments.Split(' ').ToList();
                IDiscordUser bannedUser = null;

                if (e.message.MentionedUserIds.Count > 0)
                {
                    bannedUser = await e.Guild.GetUserAsync(e.message.MentionedUserIds.First());
                }
                else
                {
                    if (arg.Count > 0)
                    {
                        bannedUser = await e.Guild.GetUserAsync(ulong.Parse(arg[0]));
                    }
                }

                if (bannedUser == null)
                {
                    await e.ErrorEmbed(e.GetResource("ban_error_user_null"))
                        .SendToChannel(e.Channel);
                    return;
                }

                if (bannedUser.Hierarchy >= e.Guild.CurrentUser.Hierarchy)
                {
                    await e.ErrorEmbed(e.GetResource("permission_error_low"))
                        .SendToChannel(e.Channel);
                    return;
                }

                arg.RemoveAt(0);

                string reason = string.Join(" ", arg);

                IDiscordEmbed embed = Utils.Embed;
                embed.Title = "⚠ SOFTBAN";
                embed.Description = $"You've been banned from **{e.Guild.Name}**!";

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    embed.AddInlineField("💬 Reason", reason);
                }

                embed.AddInlineField("💁 Banned by", e.Author.Username + "#" + e.Author.Discriminator);

                await bannedUser.SendMessage(embed);
                await bannedUser.Ban(e.Guild, 1, reason);
                await bannedUser.Unban(e.Guild);
            }
            else
            {
                await e.ErrorEmbed(e.GetResource("permission_needed_error", $"`{e.GetResource("permission_ban_members")}`"))
                    .SendToChannel(e.Channel);
            }
        }

        [Command(Name = "clean", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task CleanAsync(EventContext e)
        {
            await PruneAsync(e, _target: Bot.instance.Client.GetShardFor((e.Guild as IProxy<IGuild>).ToNativeObject()).CurrentUser.Id);
		}


        [Command(Name = "setevent", Accessibility = EventAccessibility.ADMINONLY, Aliases = new string[] { "setcommand" }, CanBeDisabled = false)]
        public async Task SetCommandAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            string[] arguments = e.arguments.Split(' ');
            ICommandEvent command = Bot.instance.Events.CommandHandler.GetCommandEvent(arguments[0]);
            if (command == null)
            {
                await Utils.ErrorEmbed(locale, $"{arguments[0]} is not a valid command").SendToChannel(e.Channel);
                return;
            }

            bool setValue = false;
            switch (arguments[1])
            {
                case "yes":
                case "y":
                case "1":
                case "true":
                    setValue = true;
                    break;
            }

            if (!command.CanBeDisabled)
            {
                await Utils.ErrorEmbed(locale, locale.GetString("miki_admin_cannot_disable", $"`{arguments[0]}`")).SendToChannel(e.Channel);
                return;
            }

            if (arguments.Length > 2)
            {
                if (arguments.Contains("-s"))
                {
                }
            }
            await command.SetEnabled(e.Channel.Id, setValue);
            await Utils.SuccessEmbed(locale, ((setValue) ? locale.GetString("miki_generic_enabled") : locale.GetString("miki_generic_disabled")) + $" {command.Name}").SendToChannel(e.Channel);
        }

        [Command(Name = "setmodule", Accessibility = EventAccessibility.ADMINONLY, CanBeDisabled = false)]
        public async Task SetModuleAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            string[] arguments = e.arguments.Split(' ');
            IModule m = Bot.instance.Events.GetModuleByName(arguments[0]);
            if (m == null)
            {
                await Utils.ErrorEmbed(locale, $"{arguments[0]} is not a valid module.").SendToChannel(e.Channel);
                return;
            }

            bool setValue = false;
            switch (arguments[1])
            {
                case "yes":
                case "y":
                case "1":
                case "true":
                    setValue = true;
                    break;
            }

            if (!m.CanBeDisabled && !setValue)
            {
                await Utils.ErrorEmbed(locale, locale.GetString("miki_admin_cannot_disable", $"`{arguments[0]}`")).SendToChannel(e.Channel);
                return;
            }

            if (arguments.Length > 2)
            {
                if (arguments.Contains("-s"))
                {
                    // todo: create override for all channels
                }
            }
            await m.SetEnabled(e.Channel.Id, setValue);
            await Utils.SuccessEmbed(locale, ((setValue) ? locale.GetString("miki_generic_enabled") : locale.GetString("miki_generic_disabled")) + $" {m.Name}").SendToChannel(e.Channel);
        }

        [Command(Name = "kick", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task KickAsync(EventContext e)
        {
            if (e.Guild.CurrentUser.HasPermissions(e.Channel, DiscordGuildPermission.KickMembers))
            {
                List<string> arg = e.arguments.Split(' ').ToList();

                for(int i = 0; i < arg.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(arg[i]))
                    {
                        arg.RemoveAt(i);
                        i--;
                    }
                }

                IDiscordUser bannedUser = null;
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                if (e.message.MentionedUserIds.Count > 0)
                {
                    bannedUser = await e.Guild.GetUserAsync(e.message.MentionedUserIds.First());
                }
                else
                {
                    if (arg.Count > 0)
                    {
                        bannedUser = await e.Guild.GetUserAsync(ulong.Parse(arg[0]));
                    }
                }

                if (bannedUser == null)
                {
                    await e.ErrorEmbed(e.GetResource("ban_error_user_null"))
                        .SendToChannel(e.Channel);
                    return;
                }

                if (bannedUser.Hierarchy >= e.Guild.CurrentUser.Hierarchy)
                {
                    await e.ErrorEmbed(e.GetResource("permission_error_low"))
                        .SendToChannel(e.Channel);
                    return;
                }

                arg.RemoveAt(0);

                string reason = string.Join(" ", arg);

                IDiscordEmbed embed = Utils.Embed;
                embed.Title = locale.GetString("miki_module_admin_kick_header");
                embed.Description = locale.GetString("miki_module_admin_kick_description", new object[] { e.Guild.Name });

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    embed.AddInlineField(locale.GetString("miki_module_admin_kick_reason"), reason);
                }

                embed.AddInlineField(locale.GetString("miki_module_admin_kick_by"), e.Author.Username + "#" + e.Author.Discriminator);

                embed.Color = new IA.SDK.Color(1, 1, 0);

                await bannedUser.SendMessage(embed);
                await bannedUser.Kick(reason);
            }
            else
            {
                await e.ErrorEmbed(e.GetResource("permission_needed_error", $"`{e.GetResource("permission_kick_members")}`"))
                    .SendToChannel(e.Channel);
            }
        }

        [Command(Name = "prune", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task PruneAsync( EventContext e )
        {
			await PruneAsync(e, 100, 0);
		}

		public async Task PruneAsync( EventContext e, int _amount = 100, ulong _target = 0 )
		{
			Locale locale = Locale.GetEntity( e.Channel.Id.ToDbLong() );

			IDiscordUser invoker = await e.Guild.GetUserAsync( Bot.instance.Client.GetShard( 0 ).CurrentUser.Id );
			if( !invoker.HasPermissions( e.Channel, DiscordGuildPermission.ManageMessages ) )
			{
				await e.Channel.SendMessage( locale.GetString( "miki_module_admin_prune_error_no_access" ) );
				return;
			}

			int amount = _amount;
			string[] argsSplit = e.arguments.Split( ' ' );
			ulong target = e.message.MentionedUserIds.Count > 0 ? ( await e.Guild.GetUserAsync( e.message.MentionedUserIds.First() ) ).Id : _target;

			if( !string.IsNullOrEmpty( argsSplit[0] ) )
			{
				if( int.TryParse( argsSplit[0], out amount ) )
				{
					if( amount < 0 )
					{
						IDiscordEmbed errorMessage = Utils.ErrorEmbed( e, locale.GetString( "miki_module_admin_prune_error_negative" ) );
						await errorMessage.SendToChannel( e.Channel );
						return;
					}
					if( amount > 100 )
					{
						IDiscordEmbed errorMessage = Utils.ErrorEmbed( e, locale.GetString( "miki_module_admin_prune_error_max" ) );
						await errorMessage.SendToChannel( e.Channel );
						return;
					}
				}
				else
				{
					IDiscordEmbed errorMessage = Utils.ErrorEmbed( e, locale.GetString( "miki_module_admin_prune_error_parse" ) );
					await errorMessage.SendToChannel( e.Channel );
					return;
				}
			}

			await e.message.DeleteAsync(); // Delete the calling message before we get the message history.

			List<IDiscordMessage> messages = await e.Channel.GetMessagesAsync( amount );
			List<IDiscordMessage> deleteMessages = new List<IDiscordMessage>();

			if( messages.Count < amount )
			{
				amount = messages.Count; // Checks if the amount of messages to delete is more than the amount of messages availiable.
			}

			if( amount <= 1 )
			{
				string prefix = await PrefixInstance.Default.GetForGuildAsync( e.Guild.Id );
				await e.message.DeleteAsync();
				IDiscordEmbed errorMessage = Utils.ErrorEmbed( e, locale.GetString( "miki_module_admin_prune_no_messages", new object[] { prefix } ) );
				await errorMessage.SendToChannel( e.Channel );
				return;
			}

			for( int i = 0; i < amount; i++ )
			{
				if( target != 0 && messages[i]?.Author.Id != target )
					continue;

				if( messages[i].Timestamp.AddDays( 14 ) > DateTime.Now )
				{
					deleteMessages.Add( messages[i] );
				}
			}

			if( deleteMessages.Count > 0 )
			{
				await e.Channel.DeleteMessagesAsync( deleteMessages );
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
			embed.Title = titles[MikiRandom.Next( titles.Length - 1 )];
			embed.Description = e.GetResource( "miki_module_admin_prune_success", deleteMessages.Count );

			embed.Color = IA.SDK.Color.GetColor( IAColor.YELLOW );

			IDiscordMessage _dMessage = await embed.SendToChannel( e.Channel );

			await Task.Delay( 5000 );

			await _dMessage.DeleteAsync();
		}
	}
}
