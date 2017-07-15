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
    [Module("Admin")]
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
                embed.AddField(f =>
                {
                    f.Name = "💬 Reason";
                    f.Value = reason;
                    f.IsInline = true;
                });
            }

            embed.AddField(f =>
            {
                f.Name = "💁 Banned by";
                f.Value = e.Author.Username + "#" + e.Author.Discriminator;
                f.IsInline = true;
            });

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
                embed.AddField(f =>
                {
                    f.Name = "💬 Reason";
                    f.Value = reason;
                    f.IsInline = true;
                });
            }

            embed.AddField(f =>
            {
                f.Name = "💁 Banned by";
                f.Value = e.Author.Username + "#" + e.Author.Discriminator;
                f.IsInline = true;
            });

            await bannedUser.SendMessage(embed);
            await bannedUser.Ban(e.Guild);
            await bannedUser.Unban(e.Guild);

        }

        [Command(Name = "clean", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task CleanAsync(EventContext e)
        {
            await PruneAsync(e.message, 100, Bot.instance.Client.GetShard(e.message.Discord.ShardId).CurrentUser.Id);

        }

        [Command(Name = "setcommand", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SetCommandAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            string[] arguments = e.arguments.Split(' ');
            ICommandEvent command = Bot.instance.Events.CommandHandler.GetCommandEvent(arguments[0]);
            if (command == null)
            {
                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, $"{arguments[0]} is not a valid command"));
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
                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, locale.GetString("miki_admin_cannot_disable", $"`{arguments[0]}`")));
                return;
            }

            if (arguments.Length > 2)
            {
                if (arguments.Contains("-s"))
                {
                }
            }
            await command.SetEnabled(e.Channel.Id, setValue);
            await e.Channel.SendMessage(Utils.SuccessEmbed(locale, ((setValue) ? locale.GetString("miki_generic_enabled") : locale.GetString("miki_generic_disabled")) + $" {command.Name}"));

        }

        [Command(Name = "setmodule", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SetModuleAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            string[] arguments = e.arguments.Split(' ');
            IModule m = Bot.instance.Events.GetModuleByName(arguments[0]);
            if (m == null)
            {
                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, $"{arguments[0]} is not a valid module"));
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
                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, locale.GetString("miki_admin_cannot_disable", $"`{arguments[0]}`")));
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
            await e.Channel.SendMessage(Utils.SuccessEmbed(locale, ((setValue) ? locale.GetString("miki_generic_enabled") : locale.GetString("miki_generic_disabled")) + $" {m.Name}"));

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
                embed.AddField(f =>
                {
                    f.Name = locale.GetString("miki_module_admin_kick_reason");
                    f.Value = reason;
                    f.IsInline = true;
                });
            }

            embed.AddField(f =>
            {
                f.Name = locale.GetString("miki_module_admin_kick_by");
                f.Value = e.Author.Username + "#" + e.Author.Discriminator;
                f.IsInline = true;
            });

            embed.Color = new Color(1, 1, 0);

            await bannedUser.SendMessage(embed);
            await bannedUser.Kick();
        }

        [Command(Name = "prune", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task PruneAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            IDiscordUser u = (await (e.Guild.GetUserAsync(Bot.instance.Client.GetShard(0).CurrentUser.Id)));
            if (!u.HasPermissions(e.Channel, DiscordGuildPermission.ManageMessages))
            {
                await e.Channel.SendMessage(locale.GetString("miki_module_admin_prune_error_no_access"));
                return;
            }

            string[] argsSplit = e.arguments.Split(' ');
            int amount = 100;
            if (!string.IsNullOrEmpty(argsSplit[0]))
            {
                amount = int.Parse(argsSplit[0]);
                if (e.message.MentionedUserIds.Count > 0)
                {
                    await PruneAsync(e.message, amount, (await e.Guild.GetUserAsync(e.message.MentionedUserIds.First())).Id);
                    return;
                }
            }
            await PruneAsync(e.message, amount);
        }

        public async Task PruneAsync(IDiscordMessage e, int amount)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if (amount > 100)
            {
                await e.Channel.SendMessage(locale.GetString("miki_module_admin_prune_error_max"));
                return;
            }

            IEnumerable<IDiscordMessage> messages = await e.Channel.GetMessagesAsync(amount);
            List<IDiscordMessage> deleteMessages = new List<IDiscordMessage>();

            for (int i = 0; i < amount; i++)
            {
                if (messages.ElementAt(i).Timestamp.AddDays(14) > DateTime.Now)
                {
                    deleteMessages.Add(messages.ElementAt(i));
                }
            }

            if (deleteMessages.Count > 0)
            {
                await e.Channel.DeleteMessagesAsync(deleteMessages);
            }

            Task.WaitAll();

            IDiscordMessage m = await e.Channel.SendMessage(locale.GetString("miki_module_admin_prune_success", new object[] { deleteMessages.Count }));
            await Task.Delay(5000);
            await m.DeleteAsync();
        }
        public async Task PruneAsync(IDiscordMessage e, int amount, ulong target)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if (amount > 100)
            {
                await e.Channel.SendMessage(locale.GetString("miki_module_admin_prune_error_max"));
                return;
            }

            List<IDiscordMessage> messages = await e.Channel.GetMessagesAsync(amount);
            List<IDiscordMessage> deleteMessages = new List<IDiscordMessage>();

            for (int i = 0; i < messages.Count(); i++)
            {   
                if (messages.ElementAt(i)?.Author.Id == target)
                {
                    if (messages.ElementAt(i).Timestamp.AddDays(14) > DateTime.Now)
                    {
                        deleteMessages.Add(messages.ElementAt(i));
                    }
                }
            }

            if (deleteMessages.Count > 0)
            {
                await e.Channel.DeleteMessagesAsync(deleteMessages);
            }

            Task.WaitAll();

            IDiscordMessage m = await e.Channel.SendMessage(locale.GetString("miki_module_admin_prune_success", new object[] { deleteMessages.Count }));
            await Task.Delay(5000);
            await m.DeleteAsync();
        }
    }
}