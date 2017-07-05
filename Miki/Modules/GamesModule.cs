using IA;
using IA.SDK;
using IA.SDK.Events;
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

            Module i = new Module(module =>
            {
                module.Name = "Games";
                module.Events = new List<ICommandEvent>()
                {
                    new CommandEvent(cmd =>
                    {
                        cmd.Name = "roulette";
                        cmd.ProcessCommand = async (e) =>
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
