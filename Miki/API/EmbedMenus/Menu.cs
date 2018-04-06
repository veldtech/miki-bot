using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Miki.API.EmbedMenus
{
    public class Menu : IMenuItem
    {
		public IMessage Message;

		public IReadOnlyList<IMenuItem> Children => new List<IMenuItem>() { root };
		public string Name => name;
		public IMenuItem Parent => parent;
		public Menu MenuInstance => menuInstance;

		IMenuItem root;
		string name;
		IMenuItem parent;
		Menu menuInstance;

		public void Select()
		{
			root.Select();
		}
	}
}
