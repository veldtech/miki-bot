using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Miki.Modules.Gambling.Managers
{
	internal class RPSManager
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

		private List<RPSWeapon> weapons = new List<RPSWeapon>();

		public RPSManager()
		{
			weapons.Add(new RPSWeapon("scissors", emoji: ":scissors:"));
			weapons.Add(new RPSWeapon("paper", emoji: ":page_facing_up:"));
			weapons.Add(new RPSWeapon("rock", emoji: ":full_moon:"));
			RunTests();
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
				.FirstOrDefault();
		}

		public void RunTests()
		{
			Debug.Assert(CalculateVictory(0, 1) == VictoryStatus.WIN);
			Debug.Assert(CalculateVictory(1, 2) == VictoryStatus.WIN);
			Debug.Assert(CalculateVictory(2, 0) == VictoryStatus.WIN);
			Debug.Assert(CalculateVictory(0, 0) == VictoryStatus.DRAW);
			Debug.Assert(CalculateVictory(1, 1) == VictoryStatus.DRAW);
			Debug.Assert(CalculateVictory(2, 2) == VictoryStatus.DRAW);
			Debug.Assert(CalculateVictory(1, 0) == VictoryStatus.LOSE);
			Debug.Assert(CalculateVictory(2, 1) == VictoryStatus.LOSE);
			Debug.Assert(CalculateVictory(0, 2) == VictoryStatus.LOSE);
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
			return CalculateVictory(playerIndex, cpuIndex);
		}

		public VictoryStatus CalculateVictory(int player, int cpu)
		{
			return (VictoryStatus)((cpu - player + 3) % weapons.Count);
		}
	}
}