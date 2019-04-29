using Miki.Accounts;
using Miki.Cache;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands.Nodes;
using Miki.Framework.Events;
using System.Threading.Tasks;

namespace Miki.Modules.Accounts.Services
{/*
	public class ExperienceTrackerService
	{
		public ExperienceTrackerService()
		{
			Name = "Experience";
		}

		public void Install(NodeModule m)
		{
			base.Install(m);
			m.MessageRecieved += Service_MessageReceived;
		}

		public void Uninstall(NodeModule m)
		{
			base.Uninstall(m);
			m.MessageRecieved -= Service_MessageReceived;
		}

        public async Task Service_MessageReceived(IDiscordMessage m)
        {
            await AccountManager.Instance.CheckAsync(m);
        }
	}
*/}