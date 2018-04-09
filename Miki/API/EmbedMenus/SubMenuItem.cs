using Discord;
using Miki.Framework.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.EmbedMenus
{
	class SubMenuItem : BaseItem
	{
		public override IReadOnlyList<IMenuItem> Children => children;

		public List<IMenuItem> children = new List<IMenuItem>();

		public bool canGoBack = false;
	
		public override async Task SelectAsync()
		{
			Args a = await MenuInstance.ListenMessageAsync();
			ArgObject arg = a.FirstOrDefault();
			if (arg != null)
			{
				int pageId = arg.AsInt(-1);
				if(pageId != -1)
				{
					pageId = Math.Clamp(pageId, 1, Children.Count + 1);
					await (MenuInstance.Message as IUserMessage).ModifyAsync(x =>
					{	
						x.Embed = Children[pageId - 1].Build();
					});
					await Children[pageId - 1].SelectAsync();
				}
			}
		}

		public override Embed Build()
		{
			EmbedBuilder builder = new EmbedBuilder()
				.WithTitle($"{Name} | {MenuInstance.Owner}");

			for(int i = 0; i < Children.Count; i++)
			{
				builder.Description += $"`{i + 1}` - {Children[i].Name}\n";
			}

			if (canGoBack && Parent != null)
				builder.Description += $"`{Children.Count + 1}` - Back";

			return builder.Build();
		}
	}

	class PreviewItem : BaseItem
	{
		public string imageUrl = "";

		public override Embed Build()
		{
			return new EmbedBuilder()
			{
				Title = $"{name} | {MenuInstance.Owner.Username}",
				ImageUrl = imageUrl,
				Description = "do you want to buy this background for 1500 mekos? type `yes`"
			}.Build();
		}

		public override async Task SelectAsync()
		{
			await MenuInstance.ListenMessageAsync();
			await MenuInstance.Message.ModifyAsync(x =>
			{
				x.Embed = Parent.Build();
			});
			await Parent.SelectAsync();
		}
	}
}
