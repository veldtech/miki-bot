using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.SDK
{
    public class DiscordAudioClient : IDiscordAudioClient
    {
        public virtual Queue<IAudio> AudioQueue
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual bool IsPlaying
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual Task Connect(IDiscordAudioChannel channel)
        {
            throw new NotImplementedException();
        }

        public virtual Task Disconnect()
        {
            throw new NotImplementedException();
        }

        public virtual Task Pause()
        {
            throw new NotImplementedException();
        }

        public virtual Task Play(IAudio audio, bool skipIfPlaying = false)
        {
            throw new NotImplementedException();
        }

        public Task PlayFile(string file)
        {
            throw new NotImplementedException();
        }

        public virtual Task Skip()
        {
            throw new NotImplementedException();
        }
    }
}