using Discord;
using Miki.Framework.Events;
using Miki.Framework.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Miki.Framework;

namespace Miki.API.EmbedMenus
{
    public class Menu
    {
		public IUserMessage Message => message;
		public IMenuItem Root;
		public IUser Owner;

		private IUserMessage message;

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

			if(Message == null)
				throw new ArgumentNullException("Message");

			var msg = await Bot.Instance.GetAttachedObject<EventSystem>().ListenNextMessageAsync(Message.Channel.Id, Owner.Id);
			Args a = new Args(msg.Content);
			return a;
		}

		public async Task StartAsync(IMessageChannel channel)
		{
			message = await Root.Build().SendToChannel(channel);
			(Root as BaseItem).SetMenu(this);
			(Root as BaseItem).SetParent(null);
			await Root.SelectAsync();
		}
	}
}