using IA.SDK.Interfaces;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
    public class MessageAchievement : BaseAchievement
    {
        public Func<MessageEventPacket, Task<bool>> CheckMessage;

        public override async Task<bool> CheckAsync(MikiContext context, BasePacket packet)
        {
            return await CheckMessage(packet as MessageEventPacket);
        }
    }
}
