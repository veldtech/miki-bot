using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
    class CommandAchievement : BaseAchievement
    {
        public Func<CommandPacket, Task<bool>> CheckCommand;

        public override async Task<bool> CheckAsync(MikiContext context, BasePacket packet)
        {
            return await CheckCommand(packet as CommandPacket);
        }
    }
}
