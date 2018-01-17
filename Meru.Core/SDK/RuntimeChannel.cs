using Discord;
using Discord.WebSocket;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IA.SDK
{
    public class RuntimeMessageChannel : IDiscordMessageChannel, IProxy<IChannel>
    {
        public IChannel channel;

        public RuntimeMessageChannel(IChannel c)
        {
            channel = c;
        }

        public IDiscordGuild Guild
        {
            get
            {
                return new RuntimeGuild((channel as IGuildChannel).Guild);
            }
        }

        public ulong Id
        {
            get
            {
                return channel.Id;
            }
        }

        public string Name
        {
            get
            {
                return channel.Name;
            }
        }

        public bool Nsfw => channel.IsNsfw;

        public async Task DeleteMessagesAsync(List<IDiscordMessage> messages)
        {
            List<IMessage> m = new List<IMessage>();

            foreach (IDiscordMessage msg in messages)
            {
                m.Add((msg as IProxy<IMessage>).ToNativeObject());
            }

            await (channel as IMessageChannel).DeleteMessagesAsync(m);
        }

        public async Task<List<IDiscordMessage>> GetMessagesAsync(int amount = 100)
        {
            IEnumerable<IMessage> users = await (channel as IMessageChannel).GetMessagesAsync(amount).Flatten();
            List<IDiscordMessage> outputUsers = new List<IDiscordMessage>();

            foreach (IMessage u in users)
            {
                outputUsers.Add(new RuntimeMessage(u));
            }

            return outputUsers;
        }

        public async Task<List<IDiscordUser>> GetUsersAsync()
        {
            IEnumerable<IUser> users = await channel.GetUsersAsync().Flatten();
            List<IDiscordUser> outputUsers = new List<IDiscordUser>();

            foreach (IUser u in users)
            {
                outputUsers.Add(new RuntimeUser(u));
            }

            return outputUsers; 
        }

        public async Task<IDiscordMessage> SendFileAsync(string path)
        {
            return new RuntimeMessage(await (channel as IMessageChannel).SendFileAsync(path));
        }

        public async Task<IDiscordMessage> SendFileAsync(MemoryStream stream, string extension)
        {
            return new RuntimeMessage(await (channel as IMessageChannel)?.SendFileAsync(stream, extension));
        }

        public async Task<IDiscordMessage> SendMessage(string message)
        {
            RuntimeMessage m = null;
            try
            {
                m = new RuntimeMessage(await (channel as IMessageChannel).SendMessage(message));
                Log.Message("Sent message to channel " + channel.Name);
            }
            catch (Exception ex)
            {
                Log.ErrorAt("SendMessage", "failed to send");
            }
            return m;
        }

        public async Task<IDiscordMessage> SendMessage(IDiscordEmbed embed)
        {
            RuntimeMessage m = null;
            try
            {
                Log.Message("Sent embed to channel " + channel.Name);
                m = new RuntimeMessage(
                    await (channel as IMessageChannel)
                    .SendMessageAsync("", false, (embed as IProxy<EmbedBuilder>)
                    .ToNativeObject()));
            }
            catch (Exception ex)
            {
                Log.ErrorAt("SendMessage", ex.Message);
            }
            return m;
        }

        public IChannel ToNativeObject()
        {
            return channel;
        }

        public async Task SendTypingAsync()
        {
            await (channel as IMessageChannel).TriggerTypingAsync();
        }
    }
}