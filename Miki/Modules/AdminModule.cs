using IA;
using IA.Events;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Extensions;
using IA.SDK.Interfaces;
using Miki.Languages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
    internal class AdminModule
    {
        public async Task LoadEvents(Bot bot)
        {
            RuntimeModule module = new RuntimeModule(mod =>
            {
                mod.Name = "admin";
                mod.CanBeDisabled = false;
                mod.Events = new List<ICommandEvent>()
                {
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "ban";
                        x.GuildPermissions = new List<DiscordGuildPermission> { DiscordGuildPermission.BanMembers };
                        x.Accessibility = EventAccessibility.ADMINONLY;
                        x.ProcessCommand = async(e, args) =>
                        {
                            List<string> arg = args.Split(' ').ToList();
                            IDiscordUser bannedUser = null;

                            if(e.MentionedUserIds.Count > 0)
                            {
                                bannedUser = await e.Guild.GetUserAsync(e.MentionedUserIds.First());
                            }
                            else
                            {
                                bannedUser = await e.Guild.GetUserAsync(ulong.Parse(args.Split(' ')[0]));
                            }

                            arg.RemoveAt(0);

                            string reason = string.Join(" ", arg);

                            IDiscordEmbed embed = e.CreateEmbed();
                            embed.Title = "🛑 BAN";
                            embed.Description = $"You've been banned from **{e.Guild.Name}**!";

                            if(!string.IsNullOrWhiteSpace(reason))
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
                        };
                    }),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "softban";
                        x.GuildPermissions = new List<DiscordGuildPermission> { DiscordGuildPermission.BanMembers };
                        x.Accessibility = EventAccessibility.ADMINONLY;

                        x.Metadata = new EventMetadata("Softban a person who's being a rude dude!", "I cannot softban this person!",
                            ">softban [@user]", ">softban [@user] [reason]", ">softban [userid]", ">softban [userid] [reason]");
                        x.ProcessCommand = async(e, args) =>
                        {
                            List<string> arg = args.Split(' ').ToList();
                            IDiscordUser bannedUser = null;

                            if(e.MentionedUserIds.Count > 0)
                            {
                                bannedUser = await e.Guild.GetUserAsync(e.MentionedUserIds.First());
                            }
                            else
                            {
                                bannedUser = await e.Guild.GetUserAsync(ulong.Parse(args.Split(' ')[0]));
                            }

                            arg.RemoveAt(0);

                            string reason = string.Join(" ", arg);

                            IDiscordEmbed embed = e.CreateEmbed();
                            embed.Title = "⚠ SOFTBAN";
                            embed.Description = $"You've been banned from **{e.Guild.Name}**!";

                            if(!string.IsNullOrWhiteSpace(reason))
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
                        };
                    }),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "clean";
                        x.GuildPermissions = new List<DiscordGuildPermission> { DiscordGuildPermission.ManageMessages };
                        x.Accessibility = EventAccessibility.ADMINONLY;
                        x.ProcessCommand = async (e, args) =>
                        {
                            await PruneAsync(e, 100, bot.Client.GetShard(e.Discord.ShardId).CurrentUser.Id);
                        };
                    }),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "setcommand";
                        x.CanBeDisabled = false;
                        x.Accessibility = EventAccessibility.ADMINONLY;
                        x.ProcessCommand = async (e, args) =>
                        {
                            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                            string[] arguments = args.Split(' ');
                            ICommandEvent command = Bot.instance.Events.GetCommandEvent(arguments[0]);
                            if(command == null)
                            {
                                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, $"{arguments[0]} is not a valid command"));
                                return;
                            }

                            bool setValue = false;
                            switch(arguments[1])
                            {
                                case "yes":
                                case "y":
                                case "1":
                                case "true":
                                    setValue = true;
                                    break;
                            }

                            if(!command.CanBeDisabled && !setValue)
                            {
                                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, $"{arguments[0]} cannot be disabled"));
                                return;
                            }

                            if(arguments.Length > 2)
                            {
                                if(arguments.Contains("-s"))
                                {
                                    // todo: create override for all channels
                                }
                            }
                            await command.SetEnabled(e.Channel.Id, setValue);
                            await e.Channel.SendMessage(Utils.SuccessEmbed(locale, ((setValue)?"Enabled":"Disabled") + $" {command.Name}"));
                        };
                    }),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "setmodule";
                        x.CanBeDisabled = false;
                        x.Accessibility = EventAccessibility.ADMINONLY;
                        x.ProcessCommand = async (e, args) =>
                        {
                            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                            string[] arguments = args.Split(' ');
                            IModule m = Bot.instance.Events.GetModuleByName(arguments[0]);
                            if(m == null)
                            {
                                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, $"{arguments[0]} is not a valid module"));
                                return;
                            }

                            bool setValue = false;
                            switch(arguments[1])
                            {
                                case "yes":
                                case "y":
                                case "1":
                                case "true":
                                    setValue = true;
                                    break;
                            }

                            if(!m.CanBeDisabled && !setValue) 
                            {
                                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, $"{arguments[0]} cannot be disabled"));
                                return;
                            }

                            if(arguments.Length > 2)
                            {
                                if(arguments.Contains("-s"))
                                {
                                    // todo: create override for all channels
                                }
                            }
                            await m.SetEnabled(e.Channel.Id, setValue);
                            await e.Channel.SendMessage(Utils.SuccessEmbed(locale, ((setValue)?"Enabled":"Disabled") + $" {m.Name}"));
                        };
                    }),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "kick";
                        x.Accessibility = EventAccessibility.ADMINONLY;
                        x.Metadata = new EventMetadata(
                            "Kick baddies with the power of Miki!",
                            "I do not have the permissions to kick this person :(",
                            ">kick [@user]", ">kick [@user] [reason]", ">kick [userid]", ">kick [userid] [reason]");
                        x.GuildPermissions = new List<DiscordGuildPermission> { DiscordGuildPermission.KickMembers };
                        x.ProcessCommand = async (e, args) =>
                        {
                            List<string> arg = args.Split(' ').ToList();
                            IDiscordUser bannedUser = null;
                            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                            if(e.MentionedUserIds.Count > 0)
                            {
                                bannedUser = await e.Guild.GetUserAsync(e.MentionedUserIds.First());
                            }
                            else
                            {
                                bannedUser = await e.Guild.GetUserAsync(ulong.Parse(args.Split(' ')[0]));
                            }

                            arg.RemoveAt(0);

                            string reason = string.Join(" ", arg);

                            IDiscordEmbed embed = e.CreateEmbed();
                            embed.Title = locale.GetString("miki_module_admin_kick_header");
                            embed.Description = locale.GetString("miki_module_admin_kick_description", new object[] { e.Guild.Name });

                            if(!string.IsNullOrWhiteSpace(reason))
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
                        };
                    }),
                    new RuntimeCommandEvent(x =>
                    {
                        x.Name = "prune";
                        x.GuildPermissions = new List<DiscordGuildPermission> { DiscordGuildPermission.ManageMessages };
                        x.Accessibility = EventAccessibility.ADMINONLY;
                        x.ProcessCommand = async (e, args) =>
                        {
                            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                            IDiscordUser u = (await (e.Guild.GetUserAsync(bot.Client.GetShard(0).CurrentUser.Id)));
                            if (!u.HasPermissions(e.Channel, DiscordGuildPermission.ManageMessages))
                            {
                                await e.Channel.SendMessage(locale.GetString("miki_module_admin_prune_error_no_access"));
                                return;
                            }

                            string[] argsSplit = args.Split(' ');
                            int amount = 100;
                            if (!string.IsNullOrEmpty(argsSplit[0]))
                            {
                                amount = int.Parse(argsSplit[0]);
                                if (e.MentionedUserIds.Count>0)
                                {
                                    await PruneAsync(e, amount, (await e.Guild.GetUserAsync(e.MentionedUserIds.First())).Id);
                                    return;
                                }
                            }
                            await PruneAsync(e, amount);
                        };
                    })
                };
            });

            await module.InstallAsync(bot);
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
                deleteMessages.Add(messages.ElementAt(i));
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
                    deleteMessages.Add(messages[i]);
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