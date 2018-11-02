using Miki.Discord.Common;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miki.Modules.Moderation
{
	[Module("Moderation")]
	public class ModerationModule
	{
		public ModerationModule(Module m)
		{
			m.UserJoinGuild += OnUserJoinGuild;
		}

		private async Task OnUserJoinGuild(IDiscordGuildUser arg)
		{
			var matches = Regex.Matches(arg.Username, "(^|\\s)((https?:\\/\\/)?[\\w-]+(\\.[\\w-]+)+\\.?(:\\d+)?(\\/\\S*)?)");

			if (matches.Count > 0)
			{
				await arg.KickAsync("detected url in username.");
			}
		}
	}
}