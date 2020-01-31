namespace Miki.Modules.Gaming
{
    using System.Threading.Tasks;
    using Framework;
    using Framework.Commands.Attributes;
    using Miki.Bot.Models.Exceptions;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Rest;
    using Miki.Framework.Commands;
    using Miki.Framework.Extension;
    using Miki.Utility;
    using Veld.Osu;
    using Veld.Osu.Models;

    [Module("Gaming")]
	internal class GamingModule
    {
        private readonly IOsuApiClient osuClient;

        public GamingModule(IOsuApiClient osuClient)
        {
            this.osuClient = osuClient;
        }

        [Command("osu")]
		public async Task OsuAsync(IContext e)
        {
            var username = e.GetArgumentPack().TakeRequired<string>();

            var user = await osuClient.GetPlayerAsync(username);
            if(user == null)
            {
                throw new UserNullException();
            }

            await GetOsuEmbed(e, GameMode.Osu, user)
                .QueueAsync(e, e.GetChannel());
        }

		[Command("ctb")]
		public async Task CatchTheBeatAsync(IContext e)
		{
            var username = e.GetArgumentPack().TakeRequired<string>();

            var user = await osuClient.GetPlayerAsync(username, GameMode.CatchTheBeat);
            if(user == null)
            {
                throw new UserNullException();
            }

            await GetOsuEmbed(e, GameMode.Osu, user)
                .QueueAsync(e, e.GetChannel());
        }

		[Command("mania")]
		public async Task ManiaAsync(IContext e)
		{
            var username = e.GetArgumentPack().TakeRequired<string>();

            var user = await osuClient.GetPlayerAsync(username, GameMode.Mania);
            if(user == null)
            {
                throw new UserNullException();
            }

            await GetOsuEmbed(e, GameMode.Osu, user)
                .QueueAsync(e, e.GetChannel());
        }

		[Command("taiko")]
		public async Task TaikoAsync(IContext e)
		{
            var username = e.GetArgumentPack().TakeRequired<string>();

            var user = await osuClient.GetPlayerAsync(username, GameMode.Taiko);
            if(user == null)
            {
                throw new UserNullException();
            }

            await GetOsuEmbed(e, GameMode.Osu, user)
                .QueueAsync(e, e.GetChannel());
        }

        private DiscordEmbed GetOsuEmbed(IContext context, GameMode gamemode, IOsuPlayer user)
        {
            return new EmbedBuilder()
                .SetAuthor(
                    $"{gamemode.ToString().ToLowerInvariant()}! | {user.Username}",
                    "https://cdn.miki.ai/commands/logo-osu.png",
                    $"https://osu.ppy.sh/users/{user.UserId}")
                .SetDescription(
                    $":flag_{user.Country.ToLowerInvariant()}: - {user.RankCountry:N0}, 🌐 - {user.RankGlobal:N0}")
                .AddInlineField("Accuracy", $"{user.Accuracy:N2}")
                .AddInlineField("PP", $"{user.PerformancePoints:N2}")
                .AddField("Total Time Played", user.TimePlayed.ToTimeString(context.GetLocale(), true))
                .SetThumbnail($"https://a.ppy.sh/{user.UserId}?.png")
                .SetColor(255, 102, 170)
                .ToEmbed();
        }
    }
}