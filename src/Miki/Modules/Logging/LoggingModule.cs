namespace Miki.Modules.Logging
{
    using Microsoft.EntityFrameworkCore;
    using Miki.Bot.Models;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Framework;
    using Miki.Framework.Commands.Attributes;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Framework.Extension;

    [Module("eventmessages")]
    public class LoggingModule
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

        public LoggingModule()
        {
            

            //m.UserJoinGuild = async (user) =>
            //{
            //    IDiscordGuild guild = await (user as IDiscordGuildUser).GetGuildAsync();

            //    using (var scope = MikiApp.Instance.Services.CreateScope())
            //    {
            //        List<EventMessageObject> data = await GetMessageAsync(scope.ServiceProvider.GetService<DbContext>(), guild, EventMessageType.JOINSERVER, user);
            //        if (data == null)
            //        {
            //            return;
            //        }

            //        data.ForEach(x => x.destinationChannel.QueueMessage(x.message));
            //    }
            //};

            //m.UserLeaveGuild = async (user) =>
            //{
            //    IDiscordGuild guild = await (user as IDiscordGuildUser).GetGuildAsync();
            //    using (var scope = MikiApp.Instance.Services.CreateScope())
            //    {
            //        List<EventMessageObject> data = await GetMessageAsync(scope.ServiceProvider.GetService<DbContext>(), guild, EventMessageType.LEAVESERVER, user);
            //        if (data == null)
            //        {
            //            return;
            //        }

            //        data.ForEach(x => x.destinationChannel.QueueMessage(x.message));
            //    }
            //};
        }

        // TODO (Veld): Use both Welcome message and Leave message as one function as they are too similar right now.
        [Command("setwelcomemessage")]
        public async Task SetWelcomeMessage(IContext e)
        {
            var context = e.GetService<MikiDbContext>();
            string welcomeMessage = e.GetArgumentPack().Pack.TakeAll();

            if (string.IsNullOrEmpty(welcomeMessage))
            {
                EventMessage leaveMessage = context.EventMessages.Find(e.GetChannel().Id.ToDbLong(), (short)EventMessageType.JOINSERVER);
                if (leaveMessage == null)
                {
                    await e.ErrorEmbed($"No welcome message found! To set one use: `>setwelcomemessage <message>`")
                        .ToEmbed().QueueAsync(e, e.GetChannel());
                    return;
                }

                context.EventMessages.Remove(leaveMessage);
                await e.SuccessEmbed($"Deleted your welcome message")
                    .QueueAsync(e, e.GetChannel());
            }
            else
            {
                await SetMessageAsync(context, welcomeMessage, EventMessageType.JOINSERVER, e.GetChannel().Id);
                await e.SuccessEmbed($"Your new welcome message is set to: ```{welcomeMessage}```")
                    .QueueAsync(e, e.GetChannel());
            }
            await context.SaveChangesAsync();
        }

        [Command("setleavemessage")]
        public async Task SetLeaveMessage(IContext e)
        {
            var context = e.GetService<MikiDbContext>();
            string leaveMsgString = e.GetArgumentPack().Pack.TakeAll();

            if (string.IsNullOrEmpty(leaveMsgString))
            {
                EventMessage leaveMessage = context.EventMessages.Find(e.GetChannel().Id.ToDbLong(), (short)EventMessageType.LEAVESERVER);
                if (leaveMessage == null)
                {
                    await e.ErrorEmbed($"No leave message found! To set one use: `>setleavemessage <message>`")
                        .ToEmbed().QueueAsync(e, e.GetChannel());
                    return;
                }

                context.EventMessages.Remove(leaveMessage);
                await e.SuccessEmbed($"Deleted your leave message")
                    .QueueAsync(e, e.GetChannel());

            }
            else
            {
                await SetMessageAsync(context, leaveMsgString, EventMessageType.LEAVESERVER, e.GetChannel().Id);
                await e.SuccessEmbed($"Your new leave message is set to: ```{leaveMsgString}```")
                    .QueueAsync(e, e.GetChannel());
            }
            await context.SaveChangesAsync();
        }

        [Command("testmessage")]
        public async Task TestMessage(IContext e)
        {
            var context = e.GetService<MikiDbContext>();
            if (Enum.TryParse(e.GetArgumentPack().Pack.TakeAll().ToLower(), true, out EventMessageType type))
            {
                var allmessages = await GetMessageAsync(context, e.GetGuild(), type, e.GetAuthor());
                EventMessageObject msg = allmessages.FirstOrDefault(x => x.destinationChannel.Id == e.GetChannel().Id);
                e.GetChannel().QueueMessage(e, msg.message ?? "No message set in this channel");
                return;
            }
            e.GetChannel().QueueMessage(e, $"Please pick one of these tags. ```{string.Join(',', Enum.GetNames(typeof(EventMessageType))).ToLower()}```");
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