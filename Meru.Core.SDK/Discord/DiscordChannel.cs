using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IA.SDK
{
    public class DiscordChannel : IDiscordChannel
    {
        public virtual ulong Id
        {
            get
            {
                return 0;
            }
        }

        public virtual IDiscordGuild Guild
        {
            get
            {
                return null;
            }
        }

        public string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual async Task<List<IDiscordUser>> GetUsersAsync()
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public virtual async Task SendFileAsync(string file)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public virtual async Task SendFileAsync(MemoryStream stream, string extension)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public virtual async Task<IDiscordMessage> SendMessage(string message)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public virtual async Task<IDiscordMessage> SendMessage(IDiscordEmbed embed)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }
    }
}