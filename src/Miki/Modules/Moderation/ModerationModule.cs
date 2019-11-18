namespace Miki.Modules.Moderation
{
	// todo: reimplmenent with better regex or customizability
	//[Module("Moderation")]
	//public class ModerationModule
	//{
	//	public ModerationModule(Module m)
	//	{
	//		m.UserJoinGuild += OnUserJoinGuild;
	//	}

	//	private async Task OnUserJoinGuild(IDiscordGuildUser arg)
	//	{
	//		var matches = Regex.Matches(arg.Username, "(^|\\s)((https?:\\/\\/)?[\\w-]+(\\.[\\w-]+)+\\.?(:\\d+)?(\\/\\S*)?)");

	//		if (matches.Count > 0)
	//		{
	//			await arg.KickAsync("detected url in username.");
	//		}
	//	}
	//}
}