using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.Gambling.Managers
{
	class RPSWeapon
	{
		public string Name;
		public string Emoji;

		public RPSWeapon(string name, string emoji = null)
		{
			Name = name;
			Emoji = emoji;
		}
	}
}
