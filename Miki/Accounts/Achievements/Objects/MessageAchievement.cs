using System;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
	public class MessageAchievement : BaseAchievement
	{
		public Func<MessageEventPacket, Task<bool>> CheckMessage;

		public override async Task<bool> CheckAsync(BasePacket packet)
		{
			return await CheckMessage(packet as MessageEventPacket);
		}
	}
}