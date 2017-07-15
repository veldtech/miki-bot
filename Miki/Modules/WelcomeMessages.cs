using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module(Name = "eventmessages")]
    public class EventMessageModule
    {
        /*
         * -u  = user's name
         * -um = user's mention
         * -s  = server's name
         * -o  = owner's nickname
         * -sc = server count 
         */

        public EventMessageModule(RuntimeModule m)
        {
            m.UserJoinGuild = async (guild, user) =>
            {
                List<Tuple<string, IDiscordMessageChannel>> data = await GetMessage(guild, EventMessageType.JOINSERVER, user);

                if (data == null)
                {
                    return;
                }

                data.ForEach(async x => await x.Item2.SendMessage(x.Item1));
            };

            m.UserLeaveGuild = async (guild, user) =>
            {
                List<Tuple<string, IDiscordMessageChannel>> data = await GetMessage(guild, EventMessageType.LEAVESERVER, user);

                if (data == null)
                {
                    return;
                }

                data.ForEach(async x => await x.Item2.SendMessage(x.Item1));
            };
        }

        [Command(Name = "setwelcomemessage", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SetWelcomeMessage(EventContext e)
        {
            using (var context = new MikiContext())
            {
                if (string.IsNullOrEmpty(e.arguments))
                {
                    EventMessage leaveMessage = context.EventMessages.Find(e.Channel.Id.ToDbLong(), (int)EventMessageType.JOINSERVER);
                    if (leaveMessage != null)
                    {
                        context.EventMessages.Remove(leaveMessage);
                        await e.Channel.SendMessage($"✅ deleted your welcome message");
                        await context.SaveChangesAsync();
                        return;
                    }
                    else
                    {
                        await e.Channel.SendMessage($"⚠ no welcome message found!");
                    }
                }

                if (await SetMessage(e.arguments, EventMessageType.JOINSERVER, e.Channel.Id))
                {
                    await e.Channel.SendMessage($"✅ new welcome message is set to: `{ e.arguments }`");
                }
                await context.SaveChangesAsync();
            }
        }

        [Command(Name = "setleavemessage", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SetLeaveMessage(EventContext e)
        {
            using (var context = new MikiContext())
            {
                if (string.IsNullOrEmpty(e.arguments))
                {
                    EventMessage leaveMessage = context.EventMessages.Find(e.Channel.Id.ToDbLong(), (int)EventMessageType.LEAVESERVER);
                    if (leaveMessage != null)
                    {
                        context.EventMessages.Remove(leaveMessage);
                        await e.Channel.SendMessage($"✅ deleted your leave message");
                        await context.SaveChangesAsync();
                        return;
                    }
                    else
                    {
                        await e.Channel.SendMessage($"⚠ no leave message found!");
                    }
                }

                if (await SetMessage(e.arguments, EventMessageType.LEAVESERVER, e.Channel.Id))
                {
                    await e.Channel.SendMessage($"✅ new leave message is set to: `{ e.arguments }`");
                }
                await context.SaveChangesAsync();
            }
        }

        private async Task<bool> SetMessage(string message, EventMessageType v, ulong channelid)
        {
            using (var context = new MikiContext())
            {
                EventMessage messageInstance = await context.EventMessages.FindAsync(channelid.ToDbLong(), (short)v);

                if (messageInstance == null)
                {
                    context.EventMessages.Add(new EventMessage()
                    {
                        ChannelId = channelid.ToDbLong(),
                        Message = message,
                        EventType = (short)v
                    });
                }
                else
                {
                    messageInstance.Message = message;
                }

                await context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<List<Tuple<string, IDiscordMessageChannel>>> GetMessage(IDiscordGuild guild, EventMessageType type, IDiscordUser user)
        {
            long guildId = guild.Id.ToDbLong();
            List<IDiscordMessageChannel> channels = await guild.GetChannels();
            List<Tuple<string, IDiscordMessageChannel>> output = new List<Tuple<string, IDiscordMessageChannel>>();

            using (var context = new MikiContext())
            {
                foreach (IDiscordMessageChannel c in channels)
                {
                    EventMessage messageObject = await context.EventMessages.FindAsync(c.Id.ToDbLong(), (int)type);

                    if (messageObject == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(messageObject.Message))
                    {
                        continue;
                    }

                    string modifiedMessage = messageObject.Message;

                    modifiedMessage = modifiedMessage.Replace("-um", user.Mention);
                    modifiedMessage = modifiedMessage.Replace("-u", string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname);

                    modifiedMessage = modifiedMessage.Replace("-sc", user.Guild.UserCount.ToString());
                    modifiedMessage = modifiedMessage.Replace("-s", user.Guild.Name);

                    modifiedMessage = modifiedMessage.Replace("-om", user.Guild.Owner.Mention);
                    modifiedMessage = modifiedMessage.Replace("-o", string.IsNullOrEmpty(user.Guild.Owner.Nickname) ? user.Guild.Owner.Username : user.Guild.Owner.Nickname);

                    output.Add(new Tuple<string, IDiscordMessageChannel>(modifiedMessage, c));
                }
                return output;
            }
        }
    }
}