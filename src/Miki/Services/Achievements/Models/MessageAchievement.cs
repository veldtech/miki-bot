namespace Miki.Services.Achievements.Models
{
    using System;
    using System.Threading.Tasks;
    using Miki.Accounts.Achievements.Objects;

    public class MessageAchievement : IAchievement
    {
        public Func<MessageEventPacket, ValueTask<bool>> CheckMessage;

        public string Name { get; set; }
        public string ParentName { get; set; }
        public string Icon { get; set; }
        public int Points { get; set; }

        public async ValueTask<bool> CheckAsync(BasePacket packet)
        {
            return await CheckMessage(packet as MessageEventPacket);
        }
    }
}