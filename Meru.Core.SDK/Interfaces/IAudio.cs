using System.IO;

namespace IA.SDK.Interfaces
{
    public interface IAudio
    {
        Stream AudioBytes { get; set; }
    }
}