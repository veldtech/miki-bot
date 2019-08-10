using Miki.Accounts;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands.Nodes;
using Miki.Framework.Events;
using System.Threading.Tasks;

namespace Miki.Modules.Accounts.Services
{
	public class ExperienceTrackerService
	{
		public ExperienceTrackerService()
		{
            MikiApp.Instance
                .GetService<DiscordClient>().MessageCreate += Service_MessageReceived;
        }

        public async Task Service_MessageReceived(IDiscordMessage m)
        {
            await AccountManager.Instance.CheckAsync(m);
        }
	}
}