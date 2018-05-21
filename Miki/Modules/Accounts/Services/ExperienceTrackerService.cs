using Miki.Framework.Events;
using Miki.Common;
using Miki.Accounts;
using System.Threading.Tasks;
using Discord.WebSocket;
using Miki.Framework;

namespace Miki.Modules.Accounts.Services
{
    public class ExperienceTrackerService : BaseService
    {
        public ExperienceTrackerService()
        {
            Name = "Experience";
        }

        public override void Install(Module m, Bot b)
        {
            base.Install(m, b);
            m.MessageRecieved += Service_MessageReceived;
        }

        public override void Uninstall(Module m, Bot b)
        {
            base.Uninstall(m, b);
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