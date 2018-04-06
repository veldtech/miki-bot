using System;
using System.Collections.Generic;
using System.Text;
using Discord;

namespace Miki.API.EmbedMenus
{
	class BackMenuItem : IMenuItem
	{
		public IReadOnlyList<IMenuItem> Children => throw new NotImplementedException();
		public bool HasBack => throw new NotImplementedException();

		public string Name => name;
		public IMenuItem Parent => parent;
		public Menu MenuInstance => menuInstance;

		private string name = "back";
		private IMenuItem parent = null;
		private Menu menuInstance = null;

		public BackMenuItem(Menu root, IMenuItem parent)
		{
			menuInstance = root;
			this.parent = parent;
		}

		public void Select()
		{
			Parent.Select();
		}
	}
}
