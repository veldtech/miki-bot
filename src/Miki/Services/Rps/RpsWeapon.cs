namespace Miki.Services.Rps
{
	public class RpsWeapon
	{
		public string Name;
		public string Emoji;

        public RpsWeapon(string name, string emoji = null)
		{
			Name = name;
			Emoji = emoji;
		}
	}
}