using Miki.Bot.Models.Attributes;

namespace Miki.Services.Rps
{
    [Entity("weapon")]
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