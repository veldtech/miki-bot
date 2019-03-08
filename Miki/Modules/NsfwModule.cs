using Miki.API.Imageboards;
using Miki.API.Imageboards.Enums;
using Miki.API.Imageboards.Interfaces;
using Miki.API.Imageboards.Objects;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
	[Module(Name = "nsfw", Nsfw = true)]
	internal class NsfwModule
	{
		[Command(Name = "gelbooru", Aliases = new[] { "gel" })]
		public async Task RunGelbooru(CommandContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<GelbooruPost>()
                    .GetPostAsync(e.Arguments.Pack.TakeAll(), ImageRating.EXPLICIT);

				if (!IsValid(s))
				{
                    await e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

                await CreateEmbed(s)
					.QueueToChannelAsync(e.Channel);
			}
			catch
			{
                await e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "danbooru", Aliases = new[] { "dan" })]
		public async Task DanbooruAsync(CommandContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<DanbooruPost>()
                    .GetPostAsync(e.Arguments.Pack.TakeAll(), ImageRating.EXPLICIT);

				if (!IsValid(s))
				{
                    await e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

                await CreateEmbed(s)
					.QueueToChannelAsync(e.Channel);
			}
			catch
			{
                await e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "rule34", Aliases = new[] { "r34" })]
		public async Task RunRule34(CommandContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<Rule34Post>()
                    .GetPostAsync(e.Arguments.Pack.TakeAll(), ImageRating.EXPLICIT);

				if (!IsValid(s))
				{
                    await e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

                await CreateEmbed(s)
					.QueueToChannelAsync(e.Channel);
			}
			catch
			{
                await e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "e621")]
		public async Task RunE621(CommandContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<E621Post>()
                    .GetPostAsync(e.Arguments.Pack.TakeAll(), ImageRating.EXPLICIT);

				if (!IsValid(s))
				{
                    await e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

                await CreateEmbed(s)
					.QueueToChannelAsync(e.Channel);
			}
			catch
			{
                await e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "yandere")]
		public async Task RunYandere(CommandContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<YanderePost>()
                    .GetPostAsync(e.Arguments.Pack.TakeAll(), ImageRating.EXPLICIT);

				if (!IsValid(s))
				{
                    await e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

                await CreateEmbed(s).QueueToChannelAsync(e.Channel);
			}
			catch
			{
                await e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		private DiscordEmbed CreateEmbed(ILinkable s)
		{
			string url = string.IsNullOrWhiteSpace(s.SourceUrl) ? "https://miki.ai" : s.SourceUrl;
			return new EmbedBuilder()
				.SetAuthor(s.Provider, "https://i.imgur.com/FeRu6Pw.png", url)
				.AddInlineField("Tags", FormatTags(s.Tags))
				.AddInlineField("Score", s.Score)
				.SetImage(s.Url).ToEmbed();
		}

        private static string FormatTags(string tags)
            => string.Join(", ", tags.Split(' ').Select(x => $"`x`"));

        private static bool IsValid(ILinkable s)
	        => (s != null) && (!string.IsNullOrWhiteSpace(s.Url));
	}
}