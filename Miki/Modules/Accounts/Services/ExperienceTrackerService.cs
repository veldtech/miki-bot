using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IA.SDK;
using IA.SDK.Events;
using Miki.Accounts;
using IA.Events;

namespace Miki.Modules.Accounts.Services
{
    class ExperienceTrackerService : BaseService
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
            if(await IsEnabled(m.Channel.Id))
            {
                await AccountManager.Instance.CheckAsync(m);
            }
        }
    }
}
