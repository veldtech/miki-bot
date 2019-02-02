using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.API.EmbedMenus
{
	internal class SubMenuItem : BaseItem
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
				int? pageId = arg.TakeInt();
				if (pageId != null)
				{
					pageId = Math.Clamp(pageId.Value, 1, Children.Count + 1);
					await MenuInstance.Message.EditAsync(new EditMessageArgs()
					{
						embed = Children[pageId.Value - 1].Build()
					});
					await Children[pageId.Value - 1].SelectAsync();
				}
			}
		}

		public override DiscordEmbed Build()
		{
			EmbedBuilder builder = new EmbedBuilder()
				.SetTitle($"{Name} | {MenuInstance.Owner}");

			for (int i = 0; i < Children.Count; i++)
			{
				builder.Description += $"`{i + 1}` - {Children[i].Name}\n";
			}

            if (canGoBack && Parent != null)
            {
                builder.Description += $"`{Children.Count + 1}` - Back";
            }

			return builder.ToEmbed();
		}
	}

	internal class PreviewItem : BaseItem
	{
		public string imageUrl = "";

		public override DiscordEmbed Build()
		{
			return new EmbedBuilder()
			{
				Title = $"{name} | {MenuInstance.Owner.Username}",
				ImageUrl = imageUrl,
				Description = "do you want to buy this background for 1500 mekos? type `yes`"
			}.ToEmbed();
		}

		public override async Task SelectAsync()
		{
			await MenuInstance.ListenMessageAsync();
			await MenuInstance.Message.EditAsync(new EditMessageArgs
			{
				embed = Parent.Build()
			});
			await Parent.SelectAsync();
		}
	}
}