namespace Miki.Modules.Logging
{
    using Microsoft.EntityFrameworkCore;
    using Miki.Bot.Models;
    using Miki.Discord.Common;
    using Miki.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Permissions.Attributes;
    using Miki.Framework.Commands.Permissions.Models;
    using Miki.Utility;
    using Sentry;
    using Miki.Framework.Commands.Scopes.Attributes;
    using Miki.Attributes;

    [Module("logging")]
    public class LoggingModule
    {
        private readonly ISentryClient sentryClient;
        private readonly MikiApp app;

        /**
         * -u   = user's name
         * -um  = user's mention
         * -s   = server's name
         * -o   = owner's nickname
         * -sc  = server count
		 * -now = current time
		 * -uc  = user count
         */

        public LoggingModule(MikiApp app, IDiscordClient client, ISentryClient sentryClient = null)
        {
            this.app = app;
            this.sentryClient = sentryClient;
            client.GuildMemberCreate += OnClientOnGuildMemberCreate;
            client.GuildMemberDelete += OnClientOnGuildMemberDelete;
        }

        private async Task OnClientOnGuildMemberCreate(IDiscordGuildUser user)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetService<DbContext>();
            try
            {
                var guild = await user.GetGuildAsync();
                var data = await GetMessageAsync(context, guild, EventMessageType.JOINSERVER, user);
                if(data == null)
                {
                    return;
                }

                data.ForEach(x => x.DestinationChannel.SendMessageAsync(x.Message));
            }
            catch(Exception e)
            {
                sentryClient.CaptureEvent(e.ToSentryEvent());
            }
        }

        public async Task OnClientOnGuildMemberDelete(IDiscordGuildUser user)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetService<DbContext>();
            try
            {
                var guild = await user.GetGuildAsync();
                var data = await GetMessageAsync(context, guild, EventMessageType.LEAVESERVER, user);
                if(data == null)
                {
                    return;
                }

                data.ForEach(x => x.DestinationChannel.SendMessageAsync(x.Message));
            }
            catch(Exception e)
            {
                var @event = e.ToSentryEvent();
                @event.SetTag("user.id", user?.Id.ToString() ?? "null");
                @event.SetTag("guild.id", user?.GuildId.ToString() ?? "null");
                sentryClient.CaptureEvent(e.ToSentryEvent());
            }
        }

        // TODO (Veld): Use both Welcome message and Leave message as one function as they are too similar right now.
        [Command("setwelcomemessage")]
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task SetWelcomeMessageAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();
            string welcomeMessage = e.GetArgumentPack().Pack.TakeAll();

            if(string.IsNullOrEmpty(welcomeMessage))
            {
                var leaveMessage = await context.EventMessages.FindAsync(
                    (long)e.GetChannel().Id, (short)EventMessageType.JOINSERVER);
                if(leaveMessage == null)
                {
                    await e.ErrorEmbed(
                            "No welcome message found! To set one use: `>setwelcomemessage <message>`")
                        .ToEmbed().QueueAsync(e, e.GetChannel());
                    return;
                }

                context.EventMessages.Remove(leaveMessage);
                await e.SuccessEmbed("Deleted your welcome message")
                    .QueueAsync(e, e.GetChannel());
            }
            else
            {
                await SetMessageAsync(context, welcomeMessage, EventMessageType.JOINSERVER,
                    e.GetChannel().Id);
                await e.SuccessEmbed($"Your new welcome message is set to: ```{welcomeMessage}```")
                    .QueueAsync(e, e.GetChannel());
            }

            await context.SaveChangesAsync();
        }

        [Command("setleavemessage")]
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task SetLeaveMessageAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();
            string leaveMsgString = e.GetArgumentPack().Pack.TakeAll();

            if(string.IsNullOrEmpty(leaveMsgString))
            {
                EventMessage leaveMessage = context.EventMessages.Find(e.GetChannel().Id.ToDbLong(),
                    (short)EventMessageType.LEAVESERVER);
                if(leaveMessage == null)
                {
                    await e.ErrorEmbed(
                            "No leave message found! To set one use: `>setleavemessage <message>`")
                        .ToEmbed().QueueAsync(e, e.GetChannel());
                    return;
                }

                context.EventMessages.Remove(leaveMessage);
                await e.SuccessEmbed("Deleted your leave message")
                    .QueueAsync(e, e.GetChannel());

            }
            else
            {
                await SetMessageAsync(context, leaveMsgString, EventMessageType.LEAVESERVER,
                    e.GetChannel().Id);
                await e.SuccessEmbed($"Your new leave message is set to: ```{leaveMsgString}```")
                    .QueueAsync(e, e.GetChannel());
            }

            await context.SaveChangesAsync();
        }

        [Command("testmessage")]
        public async Task TestMessageAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();
            if(Enum.TryParse(e.GetArgumentPack().Pack.TakeAll().ToLower(), true,
                out EventMessageType type))
            {
                var allmessages = await GetMessageAsync(context, e.GetGuild(), type, e.GetAuthor());
                EventMessageObject msg = allmessages.FirstOrDefault(
                    x => x.DestinationChannel.Id == e.GetChannel().Id);

                e.GetChannel().QueueMessage(e, null, msg.Message ?? "No message set in this channel");
                return;
            }

            var allOptions = string.Join(',', Enum.GetNames(typeof(EventMessageType))).ToLower();
            e.GetChannel().QueueMessage(e, null,
                $"Please pick one of these tags. ```{allOptions}```");
        }

        [Command("itestmessage")]
        [RequiresScope("developer")]
        [GuildOnly]
        public async Task TestInternalMessageAsync(IContext context)
        {
            await OnClientOnGuildMemberDelete(context.GetAuthor() as IDiscordGuildUser);
        }

        private async Task SetMessageAsync(
            DbContext db, string message, EventMessageType v, ulong channelId)
        {
            EventMessage messageInstance = await db.Set<EventMessage>()
                .FindAsync((long)channelId, (short)v);

            if(messageInstance == null)
            {
                db.Set<EventMessage>().Add(new EventMessage
                {
                    ChannelId = (long)channelId,
                    Message = message,
                    EventType = (short)v
                });
            }
            else
            {
                messageInstance.Message = message;
            }
        }

        public async Task<List<EventMessageObject>> GetMessageAsync(
            DbContext db, IDiscordGuild guild, EventMessageType type, IDiscordUser user)
        {
            var channels = (await guild.GetChannelsAsync()).ToList();
            var channelIds = channels.Select(x => (long)x.Id);

            IDiscordGuildUser owner = await guild.GetOwnerAsync();
            var ownerMention = owner.Mention;
            var ownerName = owner.Username;

            List<EventMessageObject> output = new List<EventMessageObject>();
            short t = (short)type;

            var messageObjects = await db.Set<EventMessage>()
                .Where(x => channelIds.Contains(x.ChannelId) && t == x.EventType)
                .ToListAsync();

            foreach(var c in messageObjects)
            {
                if(c == null)
                {
                    continue;
                }

                if(string.IsNullOrEmpty(c.Message))
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

                output.Add(new EventMessageObject
                {
                    Message = modifiedMessage,
                    DestinationChannel = channels.FirstOrDefault(
                        x => (long)x.Id == c.ChannelId) as IDiscordTextChannel
                });
            }

            return output;
        }
    }

    public struct EventMessageObject
    {
        public IDiscordTextChannel DestinationChannel { get; set; }
        public string Message { get; set; }
    }
}