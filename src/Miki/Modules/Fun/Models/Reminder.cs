namespace Miki.Modules.Fun
{
    using Miki.Bot.Models.Attributes;

    [Entity("reminder")]
    public class Reminder
    {
        public ulong UserId { get; set; }
        public string Context { get; set; }
    }
}
