using IA;
using IA.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    class GamesModule
    {


        public async Task LoadEvents(Bot bot)
        {
            LoadBlackjack();

            ModuleInstance i = new ModuleInstance(module =>
            {
                module.name = "Games";
                module.events = new List<CommandEvent>()
                {
                    new CommandEvent(cmd =>
                    {
                        cmd.name = "roulette";
                        cmd.processCommand = (e, arg) =>
                        {

                        };
                    }),
                };
            });
        }

        private void LoadBlackjack()
        {
            string mainText = "You have x points do you 'draw' or 'fold'";
            // draw => us drawing a new card.
            // fold => hey, lets get the dealers cards now. and see if we have lower points than the dealer.



        }
    }
}
