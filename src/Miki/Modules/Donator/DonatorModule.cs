using Microsoft.Extensions.DependencyInjection;
using Miki.Accounts.Achievements;
using Miki.Bot.Models;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Nodes;
using Miki.Framework.Events;
using Miki.Localization.Exceptions;
using Miki.Logging;
using Miki.Models;
using Miki.Modules.Donator.Exceptions;
using Miki.Rest;
using Miki.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules.Donator
{
    [Module("Donator")]
    internal class DonatorModule
    {
        private readonly Net.Http.HttpClient client;

        public DonatorModule()
        {
            var config = MikiApp.Instance.Services.GetService<Config>();

            if (!string.IsNullOrWhiteSpace(config.ImageApiUrl)
                && !string.IsNullOrWhiteSpace(config.MikiApiKey))
            {
                client = new Net.Http.HttpClient(config.ImageApiUrl)
                    .AddHeader("Authorization", config.MikiApiKey);
            }
            else
            {
                Log.Warning("Disabled Donator module due to missing configuration parameters for MikiAPI.");
            }
        }

        [Command("sellkey")]
        public async Task SellKeyAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            long id = (long) e.GetAuthor().Id;

            if (e.GetArgumentPack().Take(out Guid guid))
            {
                DonatorKey key = await DonatorKey.GetKeyAsync(context, guid);
                User u = await User.GetAsync(context, id, e.GetAuthor().Username);

                await u.AddCurrencyAsync(30000, e.GetChannel());
                context.DonatorKey.Remove(key);

                await context.SaveChangesAsync();

                await e.SuccessEmbed(e.GetLocale().GetString("key_sold_success", 30000))
                    .QueueAsync(e.GetChannel());
            }
        }

        [Command("redeemkey")]
        public async Task RedeemKeyAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            long id = (long) e.GetAuthor().Id;
            bool isValidToken = e.GetArgumentPack().Take(out Guid guid);
            if (!isValidToken)
            {
                throw new InvalidKeyFormatException();
            }

            DonatorKey key = await DonatorKey.GetKeyAsync(context, guid);
            IsDonator donatorStatus =
                await context.IsDonator.FindAsync(id) ??
                (await context.IsDonator.AddAsync(new IsDonator
                {
                    UserId = id
                })).Entity;

            donatorStatus.KeysRedeemed++;

            if (donatorStatus.ValidUntil > DateTime.Now)
            {
                donatorStatus.ValidUntil += key.StatusTime;
            }
            else
            {
                donatorStatus.ValidUntil = DateTime.Now + key.StatusTime;
            }

            await new EmbedBuilder
                {
                    Title = $"🎉 Congratulations, {e.GetAuthor().Username}",
                    Color = new Color(226, 46, 68),
                    Description =
                        $"You have successfully redeemed a donator key, I've given you **{key.StatusTime.TotalDays}** days of donator status.",
                    ThumbnailUrl = "https://i.imgur.com/OwwA5fV.png"
                }.AddInlineField("When does my status expire?", donatorStatus.ValidUntil.ToLongDateString())
                .ToEmbed().QueueAsync(e.GetChannel());

            context.DonatorKey.Remove(key);
            await context.SaveChangesAsync();

            // cheap hack.        
            var achievementManager = AchievementManager.Instance;
            var achievements = achievementManager.GetContainerById("donator").Achievements;

            if (donatorStatus.KeysRedeemed == 1)
            {
                await achievementManager.UnlockAsync(achievements[0],
                    e.GetChannel() as IDiscordTextChannel, e.GetAuthor(), 0);
            }
            else if (donatorStatus.KeysRedeemed == 5)
            {
                await achievementManager.UnlockAsync(achievements[1],
                    e.GetChannel() as IDiscordTextChannel, e.GetAuthor(), 1);
            }
            else if (donatorStatus.KeysRedeemed == 25)
            {
                await achievementManager.UnlockAsync(achievements[2],
                    e.GetChannel() as IDiscordTextChannel, e.GetAuthor(), 2);
            }
        }

        [Command("box")]
        [PatreonOnly]
        public async Task BoxAsync(IContext e)
            => await PerformCall(e,
                    $"/api/box?text={e.GetArgumentPack().Pack.TakeAll().RemoveMentions(e.GetGuild())}&url={await GetUrlFromMessageAsync(e)}")
                .ConfigureAwait(false);

        [Command("disability")]
        [PatreonOnly]
        public async Task DisabilityAsync(IContext e)
            => await PerformCall(e, "/api/disability?url=" + await GetUrlFromMessageAsync(e));

        [Command("tohru")]
        [PatreonOnly]
        public async Task TohruAsync(IContext e)
            => await PerformCall(e,
                "/api/tohru?text=" + e.GetArgumentPack().Pack.TakeAll().RemoveMentions(e.GetGuild()));

        [Command("truth")]
        [PatreonOnly]
        public async Task TruthAsync(IContext e)
            => await PerformCall(e,
                    "/api/yagami?text=" + e.GetArgumentPack().Pack.TakeAll().RemoveMentions(e.GetGuild()))
                .ConfigureAwait(false);

        [Command("trapcard")]
        [PatreonOnly]
        public async Task YugiAsync(IContext e)
            => await PerformCall(e, $"/api/yugioh?url={await GetUrlFromMessageAsync(e)}").ConfigureAwait(false);

        private async Task<string> GetUrlFromMessageAsync(IContext e)
        {
            string url = e.GetMessage().Author.GetAvatarUrl();

            if (e.GetMessage().MentionedUserIds.Count > 0)
            {
                url = (await e.GetGuild().GetMemberAsync(e.GetMessage().MentionedUserIds.First())).GetAvatarUrl();
            }

            if (e.GetMessage().Attachments.Count > 0)
            {
                url = e.GetMessage().Attachments.First().Url;
            }

            return url;
        }

        private async Task PerformCall(IContext e, string url)
        {
            Stream s = await client.GetStreamAsync(url);
            await e.GetChannel().SendFileAsync(s, "meme.png");
        }
    }
}