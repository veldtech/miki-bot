using Miki.Discord;
using Miki.Discord.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// TODO: build this into it's own library(?)
namespace Miki.API.EmbedMenus
{
	internal class BaseItem : IMenuItem
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

		public virtual DiscordEmbed Build()
		{
			return new EmbedBuilder().SetTitle($"{name} | {MenuInstance.Owner.Username}").ToEmbed();
		}

		public virtual Task SelectAsync()
		{
			throw new NotImplementedException();
		}
	}
}