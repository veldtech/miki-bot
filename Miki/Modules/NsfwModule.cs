using Meru;
using Meru.Events;
using Meru.SDK.Interfaces;
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
        public async Task LoadEvents(Client bot)
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

        public async Task RunGelbooru(IDiscordMessage msg, string args)
        {
            IPost s = GelbooruPost.Create(args, ImageRating.EXPLICIT);

            await Utils.Embed
                .SetTitle("Gelbooru")
                .SetImageUrl(s.ImageUrl)
                .SendToChannel(msg.Channel.Id);
        }

        public async Task RunRule34(IDiscordMessage msg, string args)
        {
            IPost s = Rule34Post.Create(args, ImageRating.EXPLICIT);

            await Utils.Embed
                .SetTitle("Rule34")
                .SetImageUrl(s.ImageUrl)
                .SendToChannel(msg.Channel.Id);
        }

        public async Task RunE621(IDiscordMessage msg, string args)
        {
            IPost s = E621Post.Create(args, ImageRating.EXPLICIT);

            await Utils.Embed
                .SetTitle("E621x")
                .SetImageUrl(s.ImageUrl)
                .SendToChannel(msg.Channel.Id);
        }
    }
}
