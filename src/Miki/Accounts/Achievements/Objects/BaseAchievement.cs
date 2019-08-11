using Miki.Accounts.Achievements.Objects;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Helpers;
using Miki.Models;
using System;
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