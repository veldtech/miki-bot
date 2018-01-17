namespace IA.SDK.Interfaces
{
    public interface IDiscordAttachment : IDiscordEntity
    {
        string FileName { get; }

        string Url { get; }
        string ProxyUrl { get; }

        int? Width { get; }
        int? Height { get; }

        int Size { get; }
    }
}