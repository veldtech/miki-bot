using Microsoft.EntityFrameworkCore;
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

				List<EventMessageObject> data = await GetMessage(guild, EventMessageType.JOINSERVER, user);

				if (data == null)
				{
					return;
				}

				data.ForEach(x => x.destinationChannel.QueueMessageAsync(x.message));
			};

			m.UserLeaveGuild = async (user) =>
			{
				IDiscordGuild guild = await (user as IDiscordGuildUser).GetGuildAsync();

				List<EventMessageObject> data = await GetMessage(guild, EventMessageType.LEAVESERVER, user);

				if (data == null)
				{
					return;
				}

				data.ForEach(x => x.destinationChannel.QueueMessageAsync(x.message));
			};
		}

		[Command(Name = "setwelcomemessage", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task SetWelcomeMessage(EventContext e)
		{
			using (var context = new MikiContext())
			{
				if (string.IsNullOrEmpty(e.Arguments.ToString()))
				{
					EventMessage leaveMessage = context.EventMessages.Find(e.Channel.Id.ToDbLong(), (short)EventMessageType.JOINSERVER);
					if (leaveMessage != null)
					{
						context.EventMessages.Remove(leaveMessage);
						e.Channel.QueueMessageAsync($"✅ deleted your welcome message");
						await context.SaveChangesAsync();
						return;
					}
					else
					{
						e.Channel.QueueMessageAsync($"⚠ no welcome message found!");
					}
				}

				if (await SetMessage(e.Arguments.ToString(), EventMessageType.JOINSERVER, e.Channel.Id))
				{
					e.Channel.QueueMessageAsync($"✅ new welcome message is set to: `{ e.Arguments.ToString() }`");
				}
				await context.SaveChangesAsync();
			}
		}

		[Command(Name = "setleavemessage", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task SetLeaveMessage(EventContext e)
		{
			using (var context = new MikiContext())
			{
				if (string.IsNullOrEmpty(e.Arguments.ToString()))
				{
					EventMessage leaveMessage = context.EventMessages.Find(e.Channel.Id.ToDbLong(), (short)EventMessageType.LEAVESERVER);
					if (leaveMessage != null)
					{
						context.EventMessages.Remove(leaveMessage);
						e.Channel.QueueMessageAsync($"✅ deleted your leave message");
						await context.SaveChangesAsync();
						return;
					}
					else
					{
						e.Channel.QueueMessageAsync($"⚠ no leave message found!");
					}
				}

				if (await SetMessage(e.Arguments.ToString(), EventMessageType.LEAVESERVER, e.Channel.Id))
				{
					e.Channel.QueueMessageAsync($"✅ new leave message is set to: `{ e.Arguments.ToString() }`");
				}
				await context.SaveChangesAsync();
			}
		}

		[Command(Name = "testmessage", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task TestMessage(EventContext e)
		{
			if (Enum.TryParse(e.Arguments.ToString().ToLower(), true, out EventMessageType type))
			{
				var allmessages = await GetMessage(e.Guild, type, e.Author);
				EventMessageObject msg = allmessages.FirstOrDefault(x => x.destinationChannel.Id == e.Channel.Id);
				e.Channel.QueueMessageAsync(msg.message ?? "No message set in this channel");
				return;
			}
			e.Channel.QueueMessageAsync($"Please pick one of these tags. ```{string.Join(',', Enum.GetNames(typeof(EventMessageType))).ToLower()}```");
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

		public async Task<List<EventMessageObject>> GetMessage(IDiscordGuild guild, EventMessageType type, IDiscordUser user)
		{
			var channels = await guild.GetChannelsAsync();
			var channelIds = channels.Select(x => x.Id.ToDbLong());

			IDiscordGuildUser owner = await guild.GetOwnerAsync();
			var ownerMention = owner.Mention;
			var ownerName = owner.Username;

			List<EventMessageObject> output = new List<EventMessageObject>();
			short t = (short)type;

			using (var context = new MikiContext())
			{
				var messageObjects = await context.EventMessages
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
	}

	public struct EventMessageObject
	{
		public IDiscordTextChannel destinationChannel;
		public string message;
	}
}