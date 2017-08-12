namespace Miki.Models.Interfaces
{
    public interface IDatabaseUser : IDatabaseEntity
    {
        string Name { get; set; }
    }
}