using Miki.Framework.Events;
using Miki.Common;
using Miki.Accounts;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Miki.Modules.Accounts.Services
{
    internal class ExperienceTrackerService : BaseService
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

        public async Task Service_MessageReceived(SocketMessage m)
        {
            if (await IsEnabled(m.Channel.Id))
            {
                await AccountManager.Instance.CheckAsync(m);
            }
        }
    }
}