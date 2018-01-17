using Discord;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.SDK
{
    internal class RuntimeAudioChannel : IDiscordAudioChannel
    {
        private IVoiceChannel audio;

        public RuntimeAudioChannel(IVoiceChannel a)
        {
            audio = a;
        }

        public IDiscordGuild Guild
        {
            get
            {
                return new RuntimeGuild(audio.Guild);
            }
        }

        public ulong Id
        {
            get
            {
                return audio.Id;
            }
        }

        public string Name
        {
            get
            {
                return audio.Name;
            }
        }

        public async Task<IDiscordAudioClient> ConnectAsync()
        {
            return new RuntimeAudioClient(await audio.ConnectAsync());
        }

        public Task<List<IDiscordUser>> GetUsersAsync()
        {
            throw new NotImplementedException();
        }
    }
}