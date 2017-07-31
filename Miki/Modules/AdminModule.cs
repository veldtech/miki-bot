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
            List<string> arg = e.arguments.Split(' ').ToList();
            IDiscordUser bannedUser = null;

            if (e.message.MentionedUserIds.Count > 0)
            {
                bannedUser = await e.Guild.GetUserAsync(e.message.MentionedUserIds.First());
            }
            else
            {
                bannedUser = await e.Guild.GetUserAsync(ulong.Parse(e.arguments.Split(' ')[0]));
            }

            arg.RemoveAt(0);

            string reason = string.Join(" ", arg);

            IDiscordEmbed embed = Utils.Embed;
            embed.Title = "🛑 BAN";
            embed.Description = $"You've been banned from **{e.Guild.Name}**!";

            if (!string.IsNullOrWhiteSpace(reason))
            {
                embed.AddInlineField("💬 Reason", reason);
            }

            embed.AddInlineField("💁 Banned by", e.Author.Username + "#" + e.Author.Discriminator);

            await bannedUser.SendMessage(embed);
            await bannedUser.Ban(e.Guild);
        }

        [Command(Name = "softban", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SoftbanAsync(EventContext e)
        {
            List<string> arg = e.arguments.Split(' ').ToList();
            IDiscordUser bannedUser = null;

            if (e.message.MentionedUserIds.Count > 0)
            {
                bannedUser = await e.Guild.GetUserAsync(e.message.MentionedUserIds.First());
            }
            else
            {
                bannedUser = await e.Guild.GetUserAsync(ulong.Parse(e.arguments.Split(' ')[0]));
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
            await bannedUser.Ban(e.Guild);
            await bannedUser.Unban(e.Guild);

        }

        [Command(Name = "clean", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task CleanAsync(EventContext e)
        {
            await PruneAsync(e, _target: Bot.instance.Client.GetShard(e.message.Discord.ShardId).CurrentUser.Id);

        }

        [Command(Name = "setcommand", Accessibility = EventAccessibility.ADMINONLY, CanBeDisabled = false)]
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
            List<string> arg = e.arguments.Split(' ').ToList();
            IDiscordUser bannedUser = null;
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if (e.message.MentionedUserIds.Count > 0)
            {
                bannedUser = await e.Guild.GetUserAsync(e.message.MentionedUserIds.First());
            }
            else
            {
                bannedUser = await e.Guild.GetUserAsync(ulong.Parse(e.arguments.Split(' ')[0]));
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

            embed.Color = new Color(1, 1, 0);

            await bannedUser.SendMessage(embed);
            await bannedUser.Kick();
        }
		// TODO: Add more onomatopoeia for the embed title to randomly select from for more style points.
		[Command(Name = "prune", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task PruneAsync( EventContext e ) {

			await PruneAsync( e, 100, 0 );

		}

		public async Task PruneAsync( EventContext e, int _amount = 100, ulong _target = 0 ) 
		{

			Locale locale = Locale.GetEntity( e.Channel.Id.ToDbLong() );

			IDiscordUser invoker = await e.Guild.GetUserAsync(Bot.instance.Client.GetShard(0).CurrentUser.Id);
			if( !invoker.HasPermissions( e.Channel, DiscordGuildPermission.ManageMessages ) )
			{
				await e.Channel.SendMessage( locale.GetString( "miki_module_admin_prune_error_no_access" ) );
				return;
			}

			string[] argsSplit = e.arguments.Split( ' ' );
			int amount = string.IsNullOrEmpty( argsSplit[0] ) ? _amount : int.Parse( argsSplit[0] ) + 1;
			ulong target = e.message.MentionedUserIds.Count > 0 ? ( await e.Guild.GetUserAsync( e.message.MentionedUserIds.First() ) ).Id : _target;

			if( amount > 101 )
			{
				await e.Channel.SendMessage( locale.GetString( "miki_module_admin_prune_error_max" ) );
				return;
			}
			
			List<IDiscordMessage> messages = await e.Channel.GetMessagesAsync( amount );
			List<IDiscordMessage> deleteMessages = new List<IDiscordMessage>();

			if( messages.Count < amount )
				amount = messages.Count; // Checks if the amount of messages to delete is more than the amount of messages availiable;

			Log.Warning( amount + " : " + _amount + " : " + messages.Count );

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
			embed.Title = titles[MikiRandom.GetRandomNumber( titles.Length - 1 )];
			embed.Description = locale.GetString( "miki_module_admin_prune_success", new object[] { deleteMessages.Count - 1 } );
			embed.Color = Color.GetColor( IAColor.YELLOW );

			IDiscordMessage _dMessage = await embed.SendToChannel( e.Channel );
			await Task.Delay( 5000 );
			await _dMessage.DeleteAsync();

		}
    }
}
