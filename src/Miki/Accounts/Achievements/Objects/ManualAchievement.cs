using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
	public class ManualAchievement : IAchievement
	{
		public string Name { get; set; }
		public string ParentName { get; set; }
		public string Icon { get; set; }
		public int Points { get; set; }

		public Task<bool> CheckAsync(BasePacket packet)
		{
			return Task.FromResult(true);
		}
	}
}
