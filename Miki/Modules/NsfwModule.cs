using Miki.API.Imageboards;
using Miki.API.Imageboards.Enums;
using Miki.API.Imageboards.Interfaces;
using Miki.API.Imageboards.Objects;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.UrbanDictionary;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miki.Modules
{
	[Module(Name = "nsfw", Nsfw = true)]
	internal class NsfwModule
	{
		[Command(Name = "gelbooru", Aliases = new[] { "gel" })]
		public async Task RunGelbooru(ICommandContext e)
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

        [Command(Name = "urban")]
        public async Task UrbanAsync(ICommandContext e)
        {
            if (!e.Arguments.Pack.CanTake)
            {
                return;
            }

            var api = e.GetService<UrbanDictionaryAPI>();

            var query = e.Arguments.Pack.TakeAll();
            var searchResult = await api.SearchTermAsync(query);

            if (searchResult == null)
            {
                // TODO (Veld): Something went wrong/No results found.
                return;
            }

            UrbanDictionaryEntry entry = searchResult.Entries
                .FirstOrDefault();

            if (entry != null)
            {
                string desc = Regex.Replace(entry.Definition, "\\[(.*?)\\]",
                    (x) => $"[{x.Groups[1].Value}]({api.GetUserDefinitionURL(x.Groups[1].Value)})"
                    );

                string example = Regex.Replace(entry.Example, "\\[(.*?)\\]",
                    (x) => $"[{x.Groups[1].Value}]({api.GetUserDefinitionURL(x.Groups[1].Value)})"
                    );

                await new EmbedBuilder()
                {
                    Author = new EmbedAuthor()
                    {
                        Name = "📚 " + entry.Term,
                        Url = "http://www.urbandictionary.com/define.php?term=" + query,
                    },
                    Description = e.Locale.GetString("miki_module_general_urban_author", entry.Author)
                }.AddField(e.Locale.GetString("miki_module_general_urban_definition"), desc, true)
                 .AddField(e.Locale.GetString("miki_module_general_urban_example"), example, true)
                 .AddField(e.Locale.GetString("miki_module_general_urban_rating"), "👍 " + entry.ThumbsUp.ToFormattedString() + "  👎 " + entry.ThumbsDown.ToFormattedString(), true)
                 .ToEmbed().QueueToChannelAsync(e.Channel);
            }
            else
            {
                await e.ErrorEmbed(e.Locale.GetString("error_term_invalid"))
                    .ToEmbed().QueueToChannelAsync(e.Channel);
            }
        }

        [Command(Name = "yandere")]
		public async Task RunYandere(ICommandContext e)
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