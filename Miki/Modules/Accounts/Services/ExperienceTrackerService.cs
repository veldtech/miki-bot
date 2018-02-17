using Miki.Framework.Events;
using Miki.Common;
using Miki.Common.Interfaces;
using Miki.Accounts;
using System.Threading.Tasks;

namespace Miki.Modules.Accounts.Services
{
    internal class ExperienceTrackerService : BaseService
    {
        public ExperienceTrackerService()
        {
            Name = "Experience";
        }

        public override void Install(IModule m)
        {
            base.Install(m);
            m.MessageRecieved += Service_MessageReceived;
        }

        public override void Uninstall(IModule m)
        {
            base.Uninstall(m);
            m.MessageRecieved -= Service_MessageReceived;
        }

        public async Task Service_MessageReceived(IDiscordMessage m)
        {
            if (await IsEnabled(m.Channel.Id))
            {
                await AccountManager.Instance.CheckAsync(m);
            }
        }
    }
}