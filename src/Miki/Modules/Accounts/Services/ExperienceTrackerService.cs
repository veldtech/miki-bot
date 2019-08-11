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
        private readonly AccountService _service;

		public ExperienceTrackerService(AccountService service)
		{
			MikiApp.Instance.GetService<DiscordClient>().MessageCreate += MessageReceivedAsync;

            _service = service;
		}

		private Task MessageReceivedAsync(IDiscordMessage m)
            => _service.CheckAsync(m);
    }
}