using IA;
using Miki.Modules;
using System.Threading.Tasks;

namespace Miki
{
    public class EventLoader
    {
        /// <summary>
        /// Loads all the modules.
        /// </summary>
        public static async Task LoadEvents(Bot bot)
        {
            await new AccountsModule().LoadEvents(bot);
            //await new ActionsModule().LoadEvents(bot);
            await new AdminModule().LoadEvents(bot);
            await new DeveloperModule().LoadEvents(bot);
            await new EventMessageModule().LoadEvents(bot);
            //await new GeneralModule().LoadEvents(bot);
            await new FunModule().LoadEvents(bot);
            await new NsfwModule().LoadEvents(bot);
            await new PastaModule().LoadEvents(bot);
            await new PatreonModule().LoadEvents(bot);
            await new ReactionsModule().LoadEvents(bot);
            await new ServerCountModule().LoadEvents(bot);
            await new SettingsModule().LoadEvents(bot);
        }
    }
}