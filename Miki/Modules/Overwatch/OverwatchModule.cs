using IA.Events.Attributes;
using IA.SDK.Events;
using Miki.Modules.Overwatch.API;
using Miki.Modules.Overwatch.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.Overwatch
{
    [Module("Overwatch")]
    class OverwatchModule
    {
        [Command(Name = "overwatchuser", Aliases = new string[] { "owuser" })]
        public async Task OverwatchStatsAsync(EventContext e)
        {
            OverwatchUserResponse user = await InternalGetUser(e);
        }

        public async Task<OverwatchUserResponse> InternalGetUser(EventContext e)
        {
            string[] arguments = e.arguments.Split(' ');

            string[] toggles = arguments
                .Where(x => x.StartsWith("-"))
                .ToArray();

            string[] username = arguments
                .Where(x => x.Contains("#"))
                .FirstOrDefault()
                .Split('#');

            if (username.Length <= 1)
            {
                // no discriminator
                return null;
            }

            if (int.TryParse(username[1], out int descriminator))
            {
                string name = username[0];
                OverwatchUserResponse user = await OverwatchAPI.GetUser(name, descriminator);
                return user;
            }
            else
            {
                // no discriminator
                return null;
            }
        }
    }
}
