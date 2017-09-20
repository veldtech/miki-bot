using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IA;

namespace Miki.Modules.Gambling.Managers
{
	class RPSManager
	{
		public enum VictoryStatus
		{
			WIN,
			DRAW,
			LOSE
		}

		List<RPSWeapon> weapons = new List<RPSWeapon>();

		public RPSManager()
		{
			weapons.Add( new RPSWeapon( "scissors", Aliases: new string[] { "s" }, Emoji: ":scissors:" ) );
			weapons.Add( new RPSWeapon( "paper", Aliases: new string[] { "p" }, Emoji: ":page_facing_up:" ) );
			weapons.Add( new RPSWeapon( "rock", Aliases: new string[] { "r" }, Emoji: ":full_moon:" ) );
		}

		public string[] GetAllWeapons()
		{
			string[] returnWeapons = new string[weapons.Count() - 1];
			for( int i = 0; i < weapons.Count(); i++ )
				returnWeapons[i] = weapons[i].name;

			return returnWeapons;
		}

		public RPSWeapon GetRandomWeapon()
		{
			return weapons[MikiRandom.Next( weapons.Count() )];
		}

		public RPSWeapon GetWeaponFromString( string name )
		{
			return weapons.Where( weapon => weapon.name == name || ( weapon.aliases != null && weapon.aliases.Contains( name ) ) ).First();
		}

		public bool GetWeaponFromString( string name, out RPSWeapon weapon )
		{
			weapon = GetWeaponFromString( name );
			return weapon != null;
		}

		public VictoryStatus CalculateVictory( RPSWeapon challenge, RPSWeapon opponent )
		{
			int cIndex = weapons.IndexOf( challenge );
			int oIndex = weapons.IndexOf( opponent );

			// If opponent index is greater than challenger index and is also odd then win.
			// If opponent index is less than challenger index and is also even then win.
			// If challenger index is the last in the list and opponent index is the first in the list then win.
			if( ( oIndex > cIndex && oIndex % 2 != 0 ) || ( oIndex < cIndex && oIndex % 2 == 0 ) || ( cIndex == weapons.Count() - 1 && oIndex == 0 ) )
			{
				return VictoryStatus.WIN;
			}
			else
			{
				return VictoryStatus.LOSE;
			}
		}
	}
}