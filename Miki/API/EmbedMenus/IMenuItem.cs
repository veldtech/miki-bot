using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.API.EmbedMenus
{
	public interface IMenuItem
	{
		IReadOnlyList<IMenuItem> Children { get; }
		string Name { get; }
		IMenuItem Parent { get; }
		Menu MenuInstance { get; }

		void Select();
	}
}
