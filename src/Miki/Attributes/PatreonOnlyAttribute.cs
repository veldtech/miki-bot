using System.Threading.Tasks;
using Miki.Discord;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Services;
using Miki.Utility;

namespace Miki.Attributes
{
    public class PatreonOnlyAttribute : CommandRequirementAttribute
    {
        public override async Task<bool> CheckAsync(IContext e)
        {
            var context = e.GetService<IUserService>();
            if(context == null)
            {
                return false;
            }
            return await context.UserIsDonatorAsync((long)e.GetAuthor().Id);
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
