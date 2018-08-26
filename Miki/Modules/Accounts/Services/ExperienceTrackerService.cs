using Miki.Framework.Events;
using Miki.Common;
using Miki.Accounts;
using System.Threading.Tasks;
using Miki.Framework;
using Miki.Discord.Common;

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
            if (await IsEnabled(Global.RedisClient, m.GetChannelAsync().Result.Id))
            {
                await AccountManager.Instance.CheckAsync(m);
            }
        }
    }
}