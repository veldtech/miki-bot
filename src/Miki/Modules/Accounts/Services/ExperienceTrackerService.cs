namespace Miki.Modules.Accounts.Services
{
    using System.Threading.Tasks;
    using Miki.Accounts;
    using Miki.Discord;
    using Miki.Discord.Common;

    public class ExperienceTrackerService
	{
        private readonly AccountService service;

		public ExperienceTrackerService(IDiscordClient app, AccountService service)
		{
			app.MessageCreate += MessageReceivedAsync;
            this.service = service;
		}

		private Task MessageReceivedAsync(IDiscordMessage m)
            => service.CheckAsync(m);
    }
}