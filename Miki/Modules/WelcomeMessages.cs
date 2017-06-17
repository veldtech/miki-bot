using Meru;
using Meru.Events;
using Meru.SDK;
using Meru.SDK.Events;
using Meru.SDK.Interfaces;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
    internal class EventMessageModule
    {
        /*
         * -u  = user's name
         * -um = user's mention
         * -s  = server's name
         * -o  = owner's nickname
         * -sc = server count 
         */

        public async Task LoadEvents(Client bot)
        {
            IModule i = new Module(module =>
            {
                module.Name = "welcomemessages";
                module.Events = new List<ICommandEvent>()
                {
                    new CommandEvent(cmd =>
                    {
                        cmd.Name = "setwelcomemessage";
                        cmd.Metadata.description = "Set a welcome message, set it to \"\" to remove it\n**Variables usable**\n-u : the person that joins\n-um: mention the person that joins\n-o : server owner's name\n-s : server's name";

                        cmd.Accessibility = EventAccessibility.ADMINONLY;
                        cmd.ProcessCommand = async (e, arg) =>
                        {
                            using(var context = new MikiContext())
                            {
                                if(string.IsNullOrEmpty(arg))
                                {
                                    context.EventMessages.Remove(context.EventMessages.Find(e.Channel.Id.ToDbLong(), (int)EventMessageType.JOINSERVER));
                                    await e.Channel.SendMessage($"✅ deleted your welcome message");
                                    await context.SaveChangesAsync();
                                    return;
                                }

                                if(await SetMessage(e.Guild.Id, arg, EventMessageType.JOINSERVER, e.Channel.Id))
                                {
                                    await e.Channel.SendMessage($"✅ new welcome message is set to: `{ arg }`");
                                }
                                await context.SaveChangesAsync();
                            }
                        };
                    }),
                    new CommandEvent(cmd =>
                    {
                        cmd.Name = "setleavemessage";
                        cmd.Accessibility = EventAccessibility.ADMINONLY;
                        cmd.ProcessCommand = async (e, arg) =>
                        {
                            using(var context = new MikiContext())
                            {
                                if(string.IsNullOrEmpty(arg))
                                {
                                    context.EventMessages.Remove(context.EventMessages.Find(e.Channel.Id.ToDbLong(), (int)EventMessageType.JOINSERVER));
                                    await e.Channel.SendMessage($"✅ deleted your welcome message");
                                    await context.SaveChangesAsync();
                                    return;
                                }

                                if(await SetMessage(e.Guild.Id, arg, EventMessageType.LEAVESERVER, e.Channel.Id))
                                {
                                    await e.Channel.SendMessage($"✅ new leave message is set to: `{ arg }`");
                                }
                                await context.SaveChangesAsync();
                            }
                        };
                    }),
                };

                module.UserJoinGuild = async (guild, user) =>
                {
                    Tuple<string, IDiscordMessageChannel> data = await GetMessage(guild.Id, EventMessageType.JOINSERVER, user);

                    if (data == null)
                    {
                        return;
                    }

                    await data.Item2.SendMessage(data.Item1);
                };

                module.UserLeaveGuild = async (guild, user) =>
                {
                    Tuple<string, IDiscordMessageChannel> data = await GetMessage(guild.Id, EventMessageType.LEAVESERVER, user);

                    if (data == null)
                    {
                        return;
                    }

                    await data.Item2.SendMessage(data.Item1);
                };
            });

            await new RuntimeModule(i).InstallAsync(bot);
        }

        private async Task<bool> SetMessage(ulong id, string message, EventMessageType v, ulong channelid)
        {
            using (var context = new MikiContext())
            {
                context.EventMessages.Add(new EventMessage()
                {
                    ChannelId = channelid.ToDbLong(),
                    Message = message,
                    EventType = (short)v
                });
                await context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<Tuple<string, IDiscordMessageChannel>> GetMessage(ulong id, EventMessageType type, IDiscordUser user)
        {
            long guildId = id.ToDbLong();

            using (var context = new MikiContext())
            {
                EventMessage messageObject = await context.EventMessages.FindAsync(guildId, (int)type);

                if(messageObject == null)
                {
                    return null;
                }

                IDiscordMessageChannel channel = (await user.Guild.GetChannels()).Find(c => c.Id.ToDbLong() == messageObject.ChannelId); 

                if (channel == null || string.IsNullOrEmpty(messageObject.Message))
                {
                    return null;
                }

                string modifiedMessage = messageObject.Message;

                modifiedMessage = modifiedMessage.Replace("-um", user.Mention);
                modifiedMessage = modifiedMessage.Replace("-u", string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname);

                modifiedMessage = modifiedMessage.Replace("-sc", user.Guild.UserCount.ToString());
                modifiedMessage = modifiedMessage.Replace("-s", user.Guild.Name);

                modifiedMessage = modifiedMessage.Replace("-om", user.Guild.Owner.Mention);
                modifiedMessage = modifiedMessage.Replace("-o", string.IsNullOrEmpty(user.Guild.Owner.Nickname) ? user.Guild.Owner.Username : user.Guild.Owner.Nickname);

                return new Tuple<string, IDiscordMessageChannel>(modifiedMessage, channel);
            }
        }
    }
}