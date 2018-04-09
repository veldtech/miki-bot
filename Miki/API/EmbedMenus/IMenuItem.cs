using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.EmbedMenus
{
	public interface IMenuItem
	{
		IReadOnlyList<IMenuItem> Children { get; }
		string Name { get; }

		IMenuItem Parent { get; set; }
		Menu MenuInstance { get; set; }

		Task SelectAsync();
		Embed Build();
	}
}
