using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements.Objects
{
    public class ManualAchievement : IAchievement
    {
        public string Name { get; set; }
        public string ParentName { get; set; }
        public string Icon { get; set; }
        public int Points { get; set; }

		public ValueTask<bool> CheckAsync(BasePacket packet)
		{
			return new ValueTask<bool>(true);
		}
	}
}
