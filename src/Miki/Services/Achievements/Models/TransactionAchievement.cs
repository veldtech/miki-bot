namespace Miki.Accounts.Achievements.Objects
{
    using System;
    using System.Threading.Tasks;

    internal class TransactionAchievement : IAchievement
	{
		public Func<TransactionPacket, ValueTask<bool>> CheckTransaction;

		public string Name { get; set; }
		public string ParentName { get; set; }
		public string Icon { get; set; }
		public int Points { get; set; }

		public async ValueTask<bool> CheckAsync(BasePacket packet)
		{
			return await CheckTransaction(packet as TransactionPacket);
		}
	}
}