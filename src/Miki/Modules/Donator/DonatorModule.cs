using Miki.Bot.Models;
using Miki.Discord;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Localization;
using Miki.Logging;
using Miki.Modules.Donator.Exceptions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Miki.Attributes;
using Miki.Discord.Common;
using Miki.Modules.Accounts.Services;
using Miki.Net.Http;
using Miki.Services.Achievements;
using Miki.Services.Transactions;
using Miki.Utility;

namespace Miki.Modules.Donator
{
    [Module("Donator"), Emoji(AppProps.Emoji.MoneyBill)]
	internal class DonatorModule
	{
		private readonly IHttpClient client;
        private const int KeyBuybackPrice = 30000;

		public DonatorModule(Config config)
		{
            if (!string.IsNullOrWhiteSpace(config.ImageApiUrl)
                && !string.IsNullOrWhiteSpace(config.MikiApiKey))
            {
                client = new HttpClient(config.ImageApiUrl)
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
			var unit = e.GetService<IUnitOfWork>();
            var transactionService = e.GetService<ITransactionService>();
            var keyRepository = unit.GetRepository<DonatorKey>();

			if(e.GetArgumentPack().Take(out Guid guid))
			{
				DonatorKey key = await DonatorKey.GetKeyAsync(keyRepository, guid);
                await keyRepository.DeleteAsync(key);
                await unit.CommitAsync();

                await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithAmount(KeyBuybackPrice)
                        .WithReceiver((long)e.GetAuthor().Id)
                        .WithSender(AppProps.Currency.BankId)
                        .Build());

                await e.SuccessEmbed(
                        e.GetLocale().GetString("key_sold_success", KeyBuybackPrice))
					.QueueAsync(e, e.GetChannel());
			}
		}

		[Command("redeemkey")]
		public async Task RedeemKeyAsync(IContext e)
		{
			var unit = e.GetService<IUnitOfWork>();

            var donatorRepository = unit.GetRepository<IsDonator>();
            var keyRepository = unit.GetRepository<DonatorKey>();
            var locale = e.GetLocale();

            long id = (long)e.GetAuthor().Id;
			if(!e.GetArgumentPack().Take(out Guid guid))
			{
				throw new InvalidKeyFormatException();
			}

			DonatorKey key = await DonatorKey.GetKeyAsync(keyRepository, guid);

            IsDonator donatorStatus = await donatorRepository.GetAsync(id);
            if (donatorStatus == null)
            {
                donatorStatus = new IsDonator
                {
                    UserId = id,
                    KeysRedeemed = 1,
                    ValidUntil = DateTime.UtcNow + key.StatusTime
                };
                await donatorRepository.AddAsync(donatorStatus);
            }
            else
            {
                donatorStatus.KeysRedeemed++;

                if(donatorStatus.ValidUntil > DateTime.Now)
                {
                    donatorStatus.ValidUntil += key.StatusTime;
                }
                else
                {
                    donatorStatus.ValidUntil = DateTime.Now + key.StatusTime;
                }
                await donatorRepository.EditAsync(donatorStatus);
            }
            await keyRepository.DeleteAsync(key);
            await unit.CommitAsync();

            await new EmbedBuilder
			{
				Title = $"🎉 {locale.GetString("common_success", e.GetAuthor().Username)}",
				Color = new Color(226, 46, 68),
				Description = locale.GetString("key_redeem_success", $"**{key.StatusTime.TotalDays}**"),
                ThumbnailUrl = "https://i.imgur.com/OwwA5fV.png"
			}.AddInlineField("When does my status expire?", donatorStatus.ValidUntil.ToLongDateString())
				.ToEmbed().QueueAsync(e, e.GetChannel());

			var achievementManager = e.GetService<AchievementService>();
			var donatorAchievement = achievementManager.GetAchievement(AchievementIds.DonatorId);
            if (donatorStatus.KeysRedeemed >= 1
                && donatorStatus.KeysRedeemed < 5)
            {
                await achievementManager.UnlockAsync(e, donatorAchievement, e.GetAuthor().Id);
            }
            else if (donatorStatus.KeysRedeemed >= 5
                     && donatorStatus.KeysRedeemed < 25)
            {
                await achievementManager.UnlockAsync(e, donatorAchievement, e.GetAuthor().Id, 1)
                    .ConfigureAwait(false);
            }
            else if (donatorStatus.KeysRedeemed >= 25)
            {
                await achievementManager.UnlockAsync(e, donatorAchievement, e.GetAuthor().Id, 2);
            }
        }

		[Command("box")]
		[PatreonOnly]
		public async Task BoxAsync(IContext e)
			=> await PerformCallAsync(e,
					$"/api/box?text={e.GetArgumentPack().Pack.TakeAll().RemoveMentionsAsync(e.GetGuild())}&url={await GetUrlFromMessageAsync(e)}")
				.ConfigureAwait(false);

		[Command("disability")]
		[PatreonOnly]
		public async Task DisabilityAsync(IContext e)
			=> await PerformCallAsync(e, "/api/disability?url=" + await GetUrlFromMessageAsync(e));

        [Command("tohru")]
        [PatreonOnly]
        public async Task TohruAsync(IContext e)
            => await PerformCallAsync(e,
                    "/api/tohru?text=" + e.GetArgumentPack().Pack.TakeAll().RemoveMentionsAsync(e.GetGuild()))
                .ConfigureAwait(false);

		[Command("truth")]
		[PatreonOnly]
		public async Task TruthAsync(IContext e)
			=> await PerformCallAsync(e,
					"/api/yagami?text=" + e.GetArgumentPack().Pack.TakeAll().RemoveMentionsAsync(e.GetGuild()))
				    .ConfigureAwait(false);

		[Command("trapcard")]
		[PatreonOnly]
		public async Task YugiAsync(IContext e)
			=> await PerformCallAsync(e, $"/api/yugioh?url={await GetUrlFromMessageAsync(e)}")
                .ConfigureAwait(false);

		private async Task<string> GetUrlFromMessageAsync(IContext e)
		{
			string url = e.GetAuthor().GetAvatarUrl();

            // TODO(velddev): Refactor this part with new Miki.Framework.Arguments 
			if(e.GetMessage().MentionedUserIds.Count > 0)
			{
				url = (await e.GetGuild().GetMemberAsync(e.GetMessage().MentionedUserIds.First())).GetAvatarUrl();
			}

			if(e.GetMessage().Attachments.Count > 0)
			{
				url = e.GetMessage().Attachments.First().Url;
			}

			return url;
		}

		private async Task PerformCallAsync(IContext e, string url)
		{
			Stream s = await client.GetStreamAsync(url);
			await e.GetChannel().SendFileAsync(s, "meme.png");
		}
	}
}