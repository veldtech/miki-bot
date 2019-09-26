using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Miki.Accounts;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;

namespace Miki.Modules.Accounts.Services
{
    public class ExperienceTrackerService
	{
        private readonly AccountService _service;

		public ExperienceTrackerService(AccountService service)
		{
			MikiApp.Instance.Services.GetService<DiscordClient>().MessageCreate += MessageReceivedAsync;

            _service = service;
		}

		private Task MessageReceivedAsync(IDiscordMessage m)
            => _service.CheckAsync(m);
    }
}