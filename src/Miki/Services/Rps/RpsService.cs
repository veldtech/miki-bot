namespace Miki.Services.Rps
{
    using System.Collections.Generic;
    using System.Linq;
    using System;

    public class RpsService
	{
		public enum VictoryStatus
		{
			DRAW = 0,
			WIN = 1,
			LOSE = 2,
		}

		private readonly List<RpsWeapon> weapons = new List<RpsWeapon>();

		public RpsService()
		{
			weapons.Add(new RpsWeapon("scissors", ":scissors:"));
			weapons.Add(new RpsWeapon("paper", ":page_facing_up:"));
			weapons.Add(new RpsWeapon("rock", ":full_moon:"));
		}

		public string[] GetAllWeapons()
		{
			return weapons
				.Select(x => x.Name)
				.ToArray();
		}

		public RpsWeapon GetRandomWeapon()
		{
			return weapons[MikiRandom.Next(weapons.Count())];
		}

		public RpsWeapon Parse(string name)
		{
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException();
            }
			return weapons.FirstOrDefault(w => w.Name[0] == name[0]);
		}

		public bool TryParse(string name, out RpsWeapon weapon)
			=> (weapon = Parse(name)) != null;

		public VictoryStatus CalculateVictory(RpsWeapon player, RpsWeapon cpu)
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