using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Miki.API.EmbedMenus
{
	class BaseItem : IMenuItem
	{
		public virtual IReadOnlyList<IMenuItem> Children => null;

		public string Name => name;

		public IMenuItem Parent { get; set; }
		public Menu MenuInstance { get; set; }

		public string name;

		public void SetParent(IMenuItem parent)
		{
			Parent = parent;

			if (Children != null)
			{
				foreach (var c in Children)
				{
					(c as BaseItem).SetParent(this);
				}
			}
		}
		public void SetMenu(Menu menu)
		{
			MenuInstance = menu;

			if (Children != null)
			{
				foreach (var c in Children)
				{
					(c as BaseItem).SetMenu(menu);
				}
			}
		}

		public virtual Embed Build()
		{
			return new EmbedBuilder().WithTitle($"{name} | {MenuInstance.Owner.Username}").Build();
		}

		public virtual Task SelectAsync()
		{
			throw new NotImplementedException();
		}
	}
}
