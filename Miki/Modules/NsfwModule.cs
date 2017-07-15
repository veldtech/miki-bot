using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Languages;
using Miki.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module(Name = "nsfw", Nsfw = true)]
    class NsfwModule
    {
        [Command(Name = "gelbooru", Aliases = new string[] { "gel" })]
        public async Task RunGelbooru(EventContext e)
        {
            IPost s = GelbooruPost.Create(e.arguments, ImageRating.EXPLICIT);

            if(s == null)
            {
                await Utils.ErrorEmbed(Locale.GetEntity(e.Channel.Id), "Couldn't find anything with these tags!")
                    .SendToChannel(e.Channel);
            }

            await Utils.Embed
                .SetTitle("Gelbooru")
                .SetImageUrl(s.ImageUrl)
                .SendToChannel(e.Channel.Id);
        }

        [Command(Name = "rule34", Aliases = new string[] { "r34" })]
        public async Task RunRule34(EventContext e)
        {
            IPost s = Rule34Post.Create(e.arguments, ImageRating.EXPLICIT);

            if (s == null)
            {
                await Utils.ErrorEmbed(Locale.GetEntity(e.Channel.Id), "Couldn't find anything with these tags!")
                    .SendToChannel(e.Channel);
            }

            await Utils.Embed
                .SetTitle("Rule34")
                .SetImageUrl(s.ImageUrl)
                .SendToChannel(e.Channel.Id);
        }

        [Command(Name = "e621")]
        public async Task RunE621(EventContext e)
        {
            IPost s = E621Post.Create(e.arguments, ImageRating.EXPLICIT);

            if (s == null)
            {
                await Utils.ErrorEmbed(Locale.GetEntity(e.Channel.Id), "Couldn't find anything with these tags!")
                    .SendToChannel(e.Channel);
            }

            await Utils.Embed
                .SetTitle("E621")
                .SetImageUrl(s.ImageUrl)
                .SendToChannel(e.Channel.Id);
        }
    }
}
