using Miki.Accounts.Achievements.Objects;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements
{
    public interface IAchievement
    {
        string Name { get; set; }
        string ParentName { get; set; }
        string Icon { get; set; }
        int Points { get; set; }

        Task<bool> CheckAsync(BasePacket packet);
    }
}