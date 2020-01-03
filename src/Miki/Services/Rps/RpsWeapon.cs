namespace Miki.Services.Rps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class RpsWeapon
	{
		public string Name;
		public string Emoji;

        private static readonly IEnumerable<RpsWeapon> weapons = new List<RpsWeapon>()
        {
            new RpsWeapon("Rock", ""),
            new RpsWeapon("Paper", ""),
            new RpsWeapon("Scissors", "")
        };

        public RpsWeapon(string name, string emoji = null)
		{
			Name = name;
			Emoji = emoji;
		}

        public static RpsWeapon Parse(string name)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException();
            }

            return weapons.FirstOrDefault(x => char.ToLower(x.Name[0]) == char.ToLower(name[0]));
        }

        public static bool TryParse(string name, out RpsWeapon weapon)
            => (weapon = Parse(name)) != null;
    }
}