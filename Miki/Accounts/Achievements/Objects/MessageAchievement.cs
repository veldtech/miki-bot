using System;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
    public class MessageAchievement : IAchievement
    {
        public Func<MessageEventPacket, Task<bool>> CheckMessage;

        public string Name { get; set; }
        public string ParentName { get; set; }
        public string Icon { get; set; }
        public int Points { get; set; }

        public async Task<bool> CheckAsync(BasePacket packet)
        {
            return await CheckMessage(packet as MessageEventPacket);
        }
    }
}