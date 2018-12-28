using Miki.Accounts;
using Miki.Cache;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Events;
using System.Threading.Tasks;

namespace Miki.Modules.Accounts.Services
{
	public class ExperienceTrackerService : BaseService
	{
		public ExperienceTrackerService()
		{
			Name = "Experience";
		}

		public override void Install(Module m)
		{
			base.Install(m);
			m.MessageRecieved += Service_MessageReceived;
		}

		public override void Uninstall(Module m)
		{
			base.Uninstall(m);
			m.MessageRecieved -= Service_MessageReceived;
		}

        public async Task Service_MessageReceived(IDiscordMessage m)
        {
            await AccountManager.Instance.CheckAsync(m);
        }
	}
}