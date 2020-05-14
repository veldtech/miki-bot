using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Miki.Framework;
using Miki.Bot.Models.Exceptions;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework.Commands;
using Miki.Logging;
using Miki.Utility;
using Veld.Osu;
using Veld.Osu.Models;

namespace Miki.Modules.Gaming
{
    [Module("Gaming")]
    internal class GamingModule
    {
        private readonly IOsuApiClient osuClient;

        public GamingModule(IOsuApiClient osuClient = null)
        {
            if (osuClient == null)
            {
                Log.Warning("Osu commands will not work");
            }

            this.osuClient = osuClient;
        }

        [Command("osu")]
        public Task OsuAsync(IContext e) => GetOsuUserAsync(e, GameMode.Osu);

        [Command("ctb")]
        public Task CtbAsync(IContext e) => GetOsuUserAsync(e, GameMode.CatchTheBeat);

        [Command("mania")]
        public Task ManiaAsync(IContext e) => GetOsuUserAsync(e, GameMode.Mania);

        [Command("taiko")]
        public Task TaikoAsync(IContext e) => GetOsuUserAsync(e, GameMode.Taiko);

        private async Task GetOsuUserAsync(IContext e, GameMode mode)
        {
            var username = e.GetArgumentPack().TakeRequired<string>();

            try
            {
                var user = await osuClient.GetPlayerAsync(username, mode);
                await GetOsuEmbed(e, mode, user)
                    .QueueAsync(e, e.GetChannel());
            }
            catch(InvalidOperationException)
            {
                throw new UserNullException();
            }
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