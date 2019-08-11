using Miki.Accounts;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
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