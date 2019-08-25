using Miki.API.Imageboards;
using Miki.API.Imageboards.Enums;
using Miki.API.Imageboards.Interfaces;
using Miki.API.Imageboards.Objects;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Events;
using Miki.UrbanDictionary;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miki.Modules
{
	[Module("nsfw")]
	internal class NsfwModule
	{
		[Command("gelbooru", "gel")]
		public async Task RunGelbooru(IContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<GelbooruPost>()
                    .GetPostAsync(e.GetArgumentPack().Pack.TakeAll(), ImageRating.EXPLICIT);

				if (!IsValid(s))
				{
                    await e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueAsync(e.GetChannel());
					return;
				}

                await CreateEmbed(s)
					.QueueAsync(e.GetChannel());
			}
			catch
			{
                await e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueAsync(e.GetChannel());
			}
		}

		[Command("danbooru", "dan")]
		public async Task DanbooruAsync(IContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<DanbooruPost>()
                    .GetPostAsync(e.GetArgumentPack().Pack.TakeAll(), ImageRating.EXPLICIT);

				if (!IsValid(s))
				{
                    await e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueAsync(e.GetChannel());
					return;
				}

                await CreateEmbed(s)
					.QueueAsync(e.GetChannel());
			}
			catch
			{
                await e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueAsync(e.GetChannel());
			}
		}

		[Command("rule34", "r34")]
		public async Task RunRule34(IContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<Rule34Post>()
                    .GetPostAsync(e.GetArgumentPack().Pack.TakeAll(), ImageRating.EXPLICIT);

				if (!IsValid(s))
				{
                    await e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueAsync(e.GetChannel());
					return;
				}

                await CreateEmbed(s)
					.QueueAsync(e.GetChannel());
			}
			catch
			{
                await e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueAsync(e.GetChannel());
			}
		}

		[Command("e621")]
		public async Task RunE621(IContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<E621Post>()
                    .GetPostAsync(e.GetArgumentPack().Pack.TakeAll(), ImageRating.EXPLICIT);

				if (!IsValid(s))
				{
                    await e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueAsync(e.GetChannel());
					return;
				}

                await CreateEmbed(s)
					.QueueAsync(e.GetChannel());
			}
			catch
			{
                await e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueAsync(e.GetChannel());
			}
		}

        [Command("urban")]
        public async Task UrbanAsync(IContext e)
        {
            if (!e.GetArgumentPack().Pack.CanTake)
            {
                return;
            }

            var api = e.GetService<UrbanDictionaryAPI>();

            var query = e.GetArgumentPack().Pack.TakeAll();
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
                    Description = e.GetLocale().GetString("miki_module_general_urban_author", entry.Author)
                }.AddField(e.GetLocale().GetString("miki_module_general_urban_definition"), desc, true)
                 .AddField(e.GetLocale().GetString("miki_module_general_urban_example"), example, true)
                 .AddField(e.GetLocale().GetString("miki_module_general_urban_rating"), "👍 " + entry.ThumbsUp.ToFormattedString() + "  👎 " + entry.ThumbsDown.ToFormattedString(), true)
                 .ToEmbed().QueueAsync(e.GetChannel());
            }
            else
            {
                await e.ErrorEmbed(e.GetLocale().GetString("error_term_invalid"))
                    .ToEmbed().QueueAsync(e.GetChannel());
            }
        }

        [Command("yandere")]
		public async Task RunYandere(IContext e)
		{
			try
			{
				ILinkable s = await ImageboardProviderPool.GetProvider<YanderePost>()
                    .GetPostAsync(e.GetArgumentPack().Pack.TakeAll(), ImageRating.EXPLICIT);

				if (!IsValid(s))
				{
                    await e.ErrorEmbed("Couldn't find anything with these tags!")
						.ToEmbed().QueueAsync(e.GetChannel());
					return;
				}

                await CreateEmbed(s).QueueAsync(e.GetChannel());
			}
			catch
			{
                await e.ErrorEmbed("Too many tags for this system. sorry :(")
					.ToEmbed().QueueAsync(e.GetChannel());
			}
		}

		private DiscordEmbed CreateEmbed(ILinkable s)
            => new EmbedBuilder()
                .SetColor(216, 88, 140)
				.SetAuthor(s.Provider, "https://i.imgur.com/FeRu6Pw.png", "https://miki.ai")
				.AddInlineField("🗒 Tags", FormatTags(s.Tags))
				.AddInlineField("⬆ Score", s.Score)
                .AddInlineField("🔗 Source", $"[click here]({s.Url})")
				.SetImage(s.Url).ToEmbed();
		

        private static string FormatTags(string tags)
            => string.Join(", ", tags.Split(' ').Select(x => $"`{x}`"));

        private static bool IsValid(ILinkable s)
	        => (s != null) && (!string.IsNullOrWhiteSpace(s.Url));
	}
}