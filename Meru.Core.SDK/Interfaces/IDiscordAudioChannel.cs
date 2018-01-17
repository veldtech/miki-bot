using System.Threading.Tasks;

namespace IA.SDK.Interfaces
{
    public interface IDiscordAudioChannel : IDiscordChannel
    {
        Task<IDiscordAudioClient> ConnectAsync();
    }
}