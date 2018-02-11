using Miki.Framework;
using Miki.Framework.Events.Attributes;
using Miki.Common.Events;
using Miki.Common.Interfaces;
using Miki.Models;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module(Name = "Donator")]
    internal class DonatorModule
    {
        [Command(Name = "truth")]
        public async Task TruthAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                if (u == null)
                {
                    await SendNotADonatorError(e.Channel);
                    return;
                }

                if (u.IsDonator(context))
                {
                    using (WebClient webClient = new WebClient())
                    {
                        byte[] data = webClient.DownloadData("http://api.veld.one/yagami?text=" + e.arguments);

                        using (MemoryStream mem = new MemoryStream(data))
                        {
                            await e.Channel.SendFileAsync(mem, $"meme.png");
                        }
                    }
                }
                else
                {
                    await SendNotADonatorError(e.Channel);
                }
            }
        }

        [Command(Name = "trapcard")]
        public async Task YugiAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                if (u == null)
                {
                    await SendNotADonatorError(e.Channel);
                    return;
                }

                if (u.IsDonator(context))
                {
                    string url = e.Author.AvatarUrl;

                    if (e.message.Attachments.Count > 0)
                    {
                        url = e.message.Attachments.First().Url;
                    }

                    if (e.message.MentionedUserIds.Count > 0)
                    {
                        url = (await e.Guild.GetUserAsync(e.message.MentionedUserIds.First())).AvatarUrl;
                    }

                    using (WebClient webClient = new WebClient())
                    {
                        byte[] data = webClient.DownloadData("http://api.veld.one/yugioh?url=" + url);

                        using (MemoryStream mem = new MemoryStream(data))
                        {
                            await e.Channel.SendFileAsync(mem, $"meme.png");
                        }
                    }
                }
                else
                {
                    await SendNotADonatorError(e.Channel);
                }
            }
        }

        public async Task SendNotADonatorError(IDiscordMessageChannel channel)
        {
            Utils.Embed
                .SetTitle("Sorry!")
                .SetDescription("... but you haven't donated yet, please support us with a small donation to unlock these commands!")
                .AddInlineField("Already donated?", "Make sure to join the Miki Support server and claim your donator status!")
                .AddInlineField("Where do I donate?", "You can find our patreon at https://patreon.com/mikibot")
                .QueueToChannel(channel);
        }
    }
}