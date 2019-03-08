using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Miki.Bot.Models;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
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
         * -u   = user's name
         * -um  = user's mention
         * -s   = server's name
         * -o   = owner's nickname
         * -sc  = server count
		 * -now = current time
		 * -uc  = user count
         */

        public EventMessageModule(Module m)
        {
            m.UserJoinGuild = async (user) =>
            {
                IDiscordGuild guild = await (user as IDiscordGuildUser).GetGuildAsync();

                using (var scope = MikiApp.Instance.Services.CreateScope())
                {
                    List<EventMessageObject> data = await GetMessageAsync(scope.ServiceProvider.GetService<DbContext>(), guild, EventMessageType.JOINSERVER, user);
                    if (data == null)
                    {
                        return;
                    }

                    data.ForEach(x => x.destinationChannel.QueueMessage(x.message));
                }
            };

            m.UserLeaveGuild = async (user) =>
            {
                IDiscordGuild guild = await (user as IDiscordGuildUser).GetGuildAsync();
                using (var scope = MikiApp.Instance.Services.CreateScope())
                {
                    List<EventMessageObject> data = await GetMessageAsync(scope.ServiceProvider.GetService<DbContext>(), guild, EventMessageType.LEAVESERVER, user);
                    if (data == null)
                    {
                        return;
                    }

                    data.ForEach(x => x.destinationChannel.QueueMessage(x.message));
                }
            };
        }

        // TODO (Veld): Use both Welcome message and Leave message as one function as they are too similar right now.
        [Command(Name = "setwelcomemessage", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SetWelcomeMessage(CommandContext e)
        {
            var context = e.GetService<MikiDbContext>();
            string welcomeMessage = e.Arguments.Pack.TakeAll();

            if (string.IsNullOrEmpty(welcomeMessage))
            {
                EventMessage leaveMessage = context.EventMessages.Find(e.Channel.Id.ToDbLong(), (short)EventMessageType.JOINSERVER);
                if (leaveMessage == null)
                {
                    await e.ErrorEmbed($"No welcome message found! To set one use: `>setwelcomemessage <message>`")
                        .ToEmbed().QueueToChannelAsync(e.Channel);
                    return;
                }

                context.EventMessages.Remove(leaveMessage);
                await e.SuccessEmbed($"Deleted your welcome message")
                    .QueueToChannelAsync(e.Channel);
            }
            else
            {
                await SetMessageAsync(context, welcomeMessage, EventMessageType.JOINSERVER, e.Channel.Id);
                await e.SuccessEmbed($"Your new welcome message is set to: ```{welcomeMessage}```")
                    .QueueToChannelAsync(e.Channel);
            }
            await context.SaveChangesAsync();
        }

        [Command(Name = "setleavemessage", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SetLeaveMessage(CommandContext e)
        {
            var context = e.GetService<MikiDbContext>();
            string leaveMsgString = e.Arguments.Pack.TakeAll();

            if (string.IsNullOrEmpty(leaveMsgString))
            {
                EventMessage leaveMessage = context.EventMessages.Find(e.Channel.Id.ToDbLong(), (short)EventMessageType.LEAVESERVER);
                if (leaveMessage == null)
                {
                    await e.ErrorEmbed($"No leave message found! To set one use: `>setleavemessage <message>`")
                        .ToEmbed().QueueToChannelAsync(e.Channel);
                    return;
                }

                context.EventMessages.Remove(leaveMessage);
                await e.SuccessEmbed($"Deleted your leave message")
                    .QueueToChannelAsync(e.Channel);

            }
            else
            {
                await SetMessageAsync(context, leaveMsgString, EventMessageType.LEAVESERVER, e.Channel.Id);
                await e.SuccessEmbed($"Your new leave message is set to: ```{leaveMsgString}```")
                    .QueueToChannelAsync(e.Channel);
            }
            await context.SaveChangesAsync();
        }

        [Command(Name = "testmessage", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task TestMessage(CommandContext e)
        {
            var context = e.GetService<MikiDbContext>();
            if (Enum.TryParse(e.Arguments.Pack.TakeAll().ToLower(), true, out EventMessageType type))
            {
                var allmessages = await GetMessageAsync(context, e.Guild, type, e.Author);
                EventMessageObject msg = allmessages.FirstOrDefault(x => x.destinationChannel.Id == e.Channel.Id);
                e.Channel.QueueMessage(msg.message ?? "No message set in this channel");
                return;
            }
            e.Channel.QueueMessage($"Please pick one of these tags. ```{string.Join(',', Enum.GetNames(typeof(EventMessageType))).ToLower()}```");
        }

        private async Task SetMessageAsync(DbContext db, string message, EventMessageType v, ulong channelid)
        {
            EventMessage messageInstance = await db.Set<EventMessage>().FindAsync(channelid.ToDbLong(), (short)v);

            if (messageInstance == null)
            {
                db.Set<EventMessage>().Add(new EventMessage()
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
        }

        public async Task<List<EventMessageObject>> GetMessageAsync(DbContext db, IDiscordGuild guild, EventMessageType type, IDiscordUser user)
        {
            var channels = await guild.GetChannelsAsync();
            var channelIds = channels.Select(x => x.Id.ToDbLong());

            IDiscordGuildUser owner = await guild.GetOwnerAsync();
            var ownerMention = owner.Mention;
            var ownerName = owner.Username;

            List<EventMessageObject> output = new List<EventMessageObject>();
            short t = (short)type;

            var messageObjects = await db.Set<EventMessage>()
                .Where(x => channelIds.Contains(x.ChannelId) && t == x.EventType)
                .ToListAsync();

            foreach (var c in messageObjects)
            {
                if (c == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(c.Message))
                {
                    continue;
                }

                string modifiedMessage = c.Message;

                modifiedMessage = modifiedMessage.Replace("-um", user.Mention);
                modifiedMessage = modifiedMessage.Replace("-uc", guild.MemberCount.ToString());
                modifiedMessage = modifiedMessage.Replace("-u", user.Username);

                modifiedMessage = modifiedMessage.Replace("-now", DateTime.Now.ToShortDateString());
                modifiedMessage = modifiedMessage.Replace("-s", guild.Name);

                modifiedMessage = modifiedMessage.Replace("-om", ownerMention);
                modifiedMessage = modifiedMessage.Replace("-o", ownerName);

                modifiedMessage = modifiedMessage.Replace("-cc", channels.Count().ToString());
                modifiedMessage = modifiedMessage.Replace("-vc", channels.Count().ToString());

                output.Add(new EventMessageObject()
                {
                    message = modifiedMessage,
                    destinationChannel = channels.FirstOrDefault(x => x.Id.ToDbLong() == c.ChannelId) as IDiscordTextChannel
                });
            }
            return output;
        }
    }

	public struct EventMessageObject
	{
		public IDiscordTextChannel destinationChannel;
		public string message;
	}
}