using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.SDK.Interfaces
{
    public interface IDiscordAudioClient
    {
        Queue<IAudio> AudioQueue { get; }

        bool IsPlaying { get; }

        Task Connect(IDiscordAudioChannel channel);

        Task Disconnect();

        Task Play(IAudio audio, bool skipIfPlaying = false);

        Task PlayFile(string file);

        Task Pause();

        Task Skip();
    }
}