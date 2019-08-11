using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
	public interface IAchievement
	{
		string Name { get; set; }
		string ParentName { get; set; }
		string Icon { get; set; }
		int Points { get; set; }

		ValueTask<bool> CheckAsync(BasePacket packet);
	}
}