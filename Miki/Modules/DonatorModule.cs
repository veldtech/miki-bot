using Miki.Accounts.Achievements;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Logging;
using Miki.Models;
using Miki.Rest;
using Miki.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Miki.Bot.Models;

namespace Miki.Modules
{
	[Module(Name = "Donator")]
	internal class DonatorModule
	{
        private readonly RestClient client;

		public DonatorModule(Module m)
		{
            if(!string.IsNullOrWhiteSpace(Global.Config.ImageApiUrl) 
                && !string.IsNullOrWhiteSpace(Global.Config.MikiApiKey))
            {
                client = new RestClient(Global.Config.ImageApiUrl)
                    .AddHeader("Authorization", Global.Config.MikiApiKey);
            }
            else
            {
                m.Enabled = false;
                Log.Warning("Disabled Donator module due to missing configuration parameters for MikiAPI.");
            }
		}

        [Command(Name = "sellkey")]
        public async Task SellKeyAsync(CommandContext e)
        {
            var context = e.GetService<MikiDbContext>();

            long id = (long)e.Author.Id;

            if (e.Arguments.Take(out Guid guid))
            {
                DonatorKey key = await DonatorKey.GetKeyAsync(context, guid);
                User u = await User.GetAsync(context, id, e.Author.Username);

                await u.AddCurrencyAsync(30000, e.Channel);
                context.DonatorKey.Remove(key);

                await context.SaveChangesAsync();

                await Utils.SuccessEmbed(e, e.Locale.GetString("key_sold_success", 30000))
                    .QueueToChannelAsync(e.Channel);
            }
        }

        [Command(Name = "redeemkey")]
        public async Task RedeemKeyAsync(CommandContext e)
        {
            var context = e.GetService<MikiDbContext>();

            long id = (long)e.Author.Id;
            if (e.Arguments.Take(out Guid guid))
            {
                DonatorKey key = await DonatorKey.GetKeyAsync(context, guid);
                IsDonator donatorStatus = await context.IsDonator.FindAsync(id);

                if (donatorStatus == null)
                {
                    donatorStatus = (await context.IsDonator.AddAsync(new IsDonator()
                    {
                        UserId = id
                    })).Entity;
                }

                donatorStatus.KeysRedeemed++;

                if (donatorStatus.ValidUntil > DateTime.Now)
                {
                    donatorStatus.ValidUntil += key.StatusTime;
                }
                else
                {
                    donatorStatus.ValidUntil = DateTime.Now + key.StatusTime;
                }

                await new EmbedBuilder()
                {
                    Title = ($"🎉 Congratulations, {e.Author.Username}"),
                    Color = new Color(226, 46, 68),
                    Description = ($"You have successfully redeemed a donator key, I've given you **{key.StatusTime.TotalDays}** days of donator status."),
                    ThumbnailUrl = ("https://i.imgur.com/OwwA5fV.png")
                }.AddInlineField("When does my status expire?", donatorStatus.ValidUntil.ToLongDateString())
                    .ToEmbed().QueueToChannelAsync(e.Channel);

                context.DonatorKey.Remove(key);
                await context.SaveChangesAsync();

                // cheap hack.        
                var achievementManager = AchievementManager.Instance;
                var achievements = achievementManager.GetContainerById("donator").Achievements;

                if (donatorStatus.KeysRedeemed == 1)
                {
                    await achievementManager.UnlockAsync(achievements[0], e.Channel, e.Author, 0);
                }
                else if (donatorStatus.KeysRedeemed == 5)
                {
                    await achievementManager.UnlockAsync(achievements[1], e.Channel, e.Author, 1);
                }
                else if (donatorStatus.KeysRedeemed == 25)
                {
                    await achievementManager.UnlockAsync(achievements[2], e.Channel, e.Author, 2);
                }
            }
        }

		[Command(Name = "box")]
        [PatreonOnly]
        public async Task BoxAsync(CommandContext e)
			=> await PerformCall(e, $"/api/box?text={e.Arguments.Pack.TakeAll().RemoveMentions(e.Guild)}&url={(await GetUrlFromMessageAsync(e))}");

		[Command(Name = "disability")]
        [PatreonOnly]
        public async Task DisabilityAsync(CommandContext e)
			=> await PerformCall(e, "/api/disability?url=" + (await GetUrlFromMessageAsync(e)));

		[Command(Name = "tohru")]
        [PatreonOnly]
        public async Task TohruAsync(CommandContext e)
			=> await PerformCall(e, "/api/tohru?text=" + e.Arguments.Pack.TakeAll().RemoveMentions(e.Guild));

		[Command(Name = "truth")]
        [PatreonOnly]
        public async Task TruthAsync(CommandContext e)
			=> await PerformCall(e, "/api/yagami?text=" + e.Arguments.Pack.TakeAll().RemoveMentions(e.Guild));

		[Command(Name = "trapcard")]
        [PatreonOnly]
		public async Task YugiAsync(CommandContext e)
			=> await PerformCall(e, $"/api/yugioh?url={(await GetUrlFromMessageAsync(e))}");

		private async Task<string> GetUrlFromMessageAsync(MessageContext e)
		{
			string url = e.Author.GetAvatarUrl();

			if (e.Message.MentionedUserIds.Count > 0)
			{
				url = (await e.Guild.GetMemberAsync(e.Message.MentionedUserIds.First())).GetAvatarUrl();
			}

            //if (e.Message.Attachments.Count > 0)
            //{
            //    url = e.message.Attachments.First().Url;
            //}

            return url;
		}

        private async Task PerformCall(MessageContext e, string url)
        {
            Stream s = await client.GetStreamAsync(url);
            await (e.Channel as IDiscordTextChannel).SendFileAsync(s, "meme.png");
        }
	}
}