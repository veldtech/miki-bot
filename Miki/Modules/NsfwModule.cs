using IA.Events.Attributes;
using IA.SDK.Events;
using Miki.Languages;
using System.Threading.Tasks;
using Miki.API.Imageboards;
using Miki.API.Imageboards.Enums;
using Miki.API.Imageboards.Interfaces;
using Miki.API.Imageboards.Objects;

namespace Miki.Modules
{
    [Module(Name = "nsfw", Nsfw = true)]
    internal class NsfwModule
    {
        [Command(Name = "gelbooru", Aliases = new[] { "gel" })]
        public async Task RunGelbooru(EventContext e)
        {
            ILinkable s = ImageboardProviderPool.GetProvider<GelbooruPost>().GetPost(e.arguments, ImageboardRating.EXPLICIT);

            if (s == null)
            {
                await Utils.ErrorEmbed(Locale.GetEntity(e.Channel.Id), "Couldn't find anything with these tags!")
                    .SendToChannel(e.Channel);
            }

            await Utils.Embed
                .SetTitle("Gelbooru")
                .SetImageUrl(s.Url)
                .SendToChannel(e.Channel.Id);
        }

        [Command(Name = "rule34", Aliases = new[] { "r34" })]
        public async Task RunRule34(EventContext e)
        {
            ILinkable s = ImageboardProviderPool.GetProvider<Rule34Post>().GetPost(e.arguments, ImageboardRating.EXPLICIT);

            if (s == null)
            {
                await Utils.ErrorEmbed(Locale.GetEntity(e.Channel.Id), "Couldn't find anything with these tags!")
                    .SendToChannel(e.Channel);
            }

            await Utils.Embed
                .SetTitle("Rule34")
                .SetImageUrl(s.Url)
                .SendToChannel(e.Channel.Id);
        }

        [Command(Name = "e621")]
        public async Task RunE621(EventContext e)
        {
            ILinkable s = ImageboardProviderPool.GetProvider<E621Post>().GetPost(e.arguments, ImageboardRating.EXPLICIT);

            if (s == null)
            {
                await Utils.ErrorEmbed(Locale.GetEntity(e.Channel.Id), "Couldn't find anything with these tags!")
                    .SendToChannel(e.Channel);
            }

            await Utils.Embed
                .SetTitle("E621")
                .SetImageUrl(s.Url)
                .SendToChannel(e.Channel.Id);
        }

        [Command(Name = "yandere")]
        public async Task RunYandere(EventContext e)
        {
            ILinkable s = ImageboardProviderPool.GetProvider<YanderePost>().GetPost(e.arguments, ImageboardRating.EXPLICIT);

            if (s == null)
            {
                await Utils.ErrorEmbed(Locale.GetEntity(e.Channel.Id), "Couldn't find anything with these tags!")
                    .SendToChannel(e.Channel);
            }

            await Utils.Embed
                .SetTitle("YANDE.RE")
                .SetImageUrl(s.Url)
                .SendToChannel(e.Channel.Id);
        }
    }
}