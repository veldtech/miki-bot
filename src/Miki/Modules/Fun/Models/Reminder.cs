using Miki.Bot.Models.Attributes;

namespace Miki.Modules.Fun
{
    [Entity("reminder")]
    public class Reminder
    {
        public ulong UserId { get; set; }
        public string Context { get; set; }
    }
}
