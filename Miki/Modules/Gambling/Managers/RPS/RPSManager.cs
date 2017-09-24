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
		// safe pattern for singletons
		private static RPSManager _instance = new RPSManager();
		public static RPSManager Instance => _instance;

		public enum VictoryStatus
		{
			DRAW = 0,
			WIN = 1,
			LOSE = 2,
		}

		List<RPSWeapon> weapons = new List<RPSWeapon>();

		public RPSManager()
		{
			weapons.Add(new RPSWeapon("scissors", emoji: ":scissors:"));
			weapons.Add(new RPSWeapon("paper", emoji: ":page_facing_up:"));
			weapons.Add(new RPSWeapon("rock", emoji: ":full_moon:"));
		}

		public string[] GetAllWeapons()
		{
			return weapons
				.Select(x => x.Name)
				.ToArray();
		}

		public RPSWeapon GetRandomWeapon()
		{
			return weapons[MikiRandom.Next(weapons.Count())];
		}

		public RPSWeapon Parse(string name)
		{
			// Thanks to fuzen
			return weapons
				.Where(w => w.Name[0] == name[0])
				.First();
		}

		public bool TryParse(string name, out RPSWeapon weapon)
		{
			weapon = Parse(name);
			return weapon != null;
		}

		public VictoryStatus CalculateVictory(RPSWeapon player, RPSWeapon cpu)
		{
			int playerIndex = weapons.IndexOf(player);
			int cpuIndex = weapons.IndexOf(cpu);
			return (VictoryStatus)((cpuIndex - playerIndex + 3) % weapons.Count);
		}
	}
}