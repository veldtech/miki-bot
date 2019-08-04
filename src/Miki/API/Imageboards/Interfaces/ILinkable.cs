namespace Miki.API.Imageboards.Interfaces
{
    public interface ILinkable
    {
        string Url { get; }
        string SourceUrl { get; }
        string Tags { get; }
        string Provider { get; }
        string Score { get; }
    }
}