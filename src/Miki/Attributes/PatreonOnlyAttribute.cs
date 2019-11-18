namespace Miki.Attributes
{
    using System.Threading.Tasks;
    using Bot.Models;
    using Discord;
    using Framework;
    using Framework.Commands;
    using Framework.Extension;
    using Microsoft.EntityFrameworkCore;

    public class PatreonOnlyAttribute : CommandRequirementAttribute
    {
        public override async Task<bool> CheckAsync(IContext e)
        {
            var context = e.GetService<DbContext>();
            return await IsDonator.ForUserAsync(context, e.GetAuthor().Id);
        }

        public override async Task OnCheckFail(IContext e)
        {
            await new EmbedBuilder()
            {
                Title = "Sorry!",
                Description = "... but you haven't donated yet, please support us with a small donation to unlock these commands!",
            }.AddField("Already donated?", "Make sure to join the Miki Support server and claim your donator status!")
             .AddField("Where do I donate?", "You can find our patreon at https://patreon.com/mikibot")
             .ToEmbed()
             .QueueAsync(e, e.GetChannel());
        }
    }
}
