using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.API.EmbedMenus
{
	class SubMenuItem : IMenuItem
	{
		public IReadOnlyList<IMenuItem> Children => throw new NotImplementedException();

		public bool HasBack => throw new NotImplementedException();

		public string Name => throw new NotImplementedException();

		public IMenuItem Parent => throw new NotImplementedException();

		public Menu MenuInstance => throw new NotImplementedException();

		public void Select()
		{

		}
	}
}
