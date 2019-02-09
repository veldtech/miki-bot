using Miki.Bot.Models;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki
{
    public class PatreonOnlyAttribute : CommandRequirementAttribute
    {
        public override async Task<bool> CheckAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                return await IsDonator.ForUserAsync(context, e.Author.Id);
            }
        }
    }
}
