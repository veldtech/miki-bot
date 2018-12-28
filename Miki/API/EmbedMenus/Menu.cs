using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework.Events;
using Miki.Framework.Events.Commands;
using System;
using System.Threading.Tasks;

namespace Miki.API.EmbedMenus
{
	public class Menu
	{
		public IDiscordMessage Message => message;
		public IMenuItem Root;
		public IDiscordUser Owner;

		private IDiscordMessage message;

		public Menu(Action<Menu> builder)
		{
			builder.Invoke(this);
			Root.MenuInstance = this;
			Root.Parent = null;
		}

		public async Task<Args> ListenMessageAsync()
		{
			if (Owner == null)
				throw new ArgumentNullException("Owner");

			if (Message == null)
				throw new ArgumentNullException("Message");

			var msg = await Framework.MikiApp.Instance
                .GetService<EventSystem>()
                .GetCommandHandler<MessageListener>()
				.WaitForNextMessage(Owner.Id, (await Message.GetChannelAsync()).Id);
			Args a = new Args(msg.Content);
			return a;
		}

		public async Task StartAsync(IDiscordChannel channel)
		{
			message = await Root.Build().SendToChannel(channel);
			(Root as BaseItem).SetMenu(this);
			(Root as BaseItem).SetParent(null);
			await Root.SelectAsync();
		}
	}
}