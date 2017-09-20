using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.Gambling.Managers
{
	class RPSWeapon
	{
		public string name;
		public string[] aliases;
		public string emoji;

		public RPSWeapon( string _name, string[] Aliases = null, string Emoji = null )
		{
			name = _name;
			aliases = Aliases;
			emoji = Emoji;
		}
	}
}
