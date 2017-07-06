using IA;
using IA.Events;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    class NsfwModule
    {
        public async Task LoadEvents(Bot bot)
        {
            await new RuntimeModule("NSFW")
                .SetNsfw(true)
                .AddCommand(
                    new RuntimeCommandEvent("gelbooru")
                        .SetAliases("gel")
                        .Default(RunGelbooru))
                .AddCommand(
                    new RuntimeCommandEvent("rule34")
                        .SetAliases("r34")
                        .Default(RunRule34))
                .AddCommand(
                    new RuntimeCommandEvent("e621")
                        .Default(RunE621))
                .InstallAsync(bot);              
        }

        public async Task RunGelbooru(EventContext e)
        {
            IPost s = GelbooruPost.Create(e.arguments, ImageRating.EXPLICIT);

            await Utils.Embed
                .SetTitle("Gelbooru")
                .SetImageUrl(s.ImageUrl)
                .SendToChannel(e.Channel.Id);
        }

        public async Task RunRule34(EventContext e)
        {
            IPost s = Rule34Post.Create(e.arguments, ImageRating.EXPLICIT);

            await Utils.Embed
                .SetTitle("Rule34")
                .SetImageUrl(s.ImageUrl)
                .SendToChannel(e.Channel.Id);
        }

        public async Task RunE621(EventContext e)
        {
            IPost s = E621Post.Create(e.arguments, ImageRating.EXPLICIT);

            await Utils.Embed
                .SetTitle("E621")
                .SetImageUrl(s.ImageUrl)
                .SendToChannel(e.Channel.Id);
        }
    }
}
