using Miki.Anilist;
using Miki.Discord;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.GraphQL;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Core.Modules.Anime
{
	[Module("Anime")]
	public class AnimeModule
	{
		private readonly AnilistClient anilistClient = new AnilistClient();

		[Command(Name = "getanime")]
		public async Task GetAnimeAsync(EventContext e)
			=> await GetMediaAsync(e, false, MediaFormat.MANGA, MediaFormat.NOVEL);

		[Command(Name = "getcharacter")]
		public async Task GetCharacterAsync(EventContext e)
		{
			ICharacter character = null;
			if (e.Arguments.Take(out int characterId))
			{
				character = await anilistClient.GetCharacterAsync(characterId);
			}
			else if (e.Arguments.Take(out string arg))
			{
				character = await anilistClient.GetCharacterAsync(arg);
			}

			if (character != null)
			{
				string description = character.Description;
				if (description.Length > 1024)
				{
					description = new string(description.Take(1020).ToArray());
					description = new string(description.Take(description.LastIndexOf(' ')).ToArray()) + "...";
				}

                await new EmbedBuilder()
					.SetAuthor($"{character.FirstName} {character.LastName}", "https://anilist.co/img/logo_al.png", character.SiteUrl)
					.SetDescription(character.NativeName)
					.AddInlineField("Description", description)
					.SetColor(0, 170, 255)
					.SetThumbnail(character.LargeImageUrl)
					.SetFooter("Powered by anilist.co", "")
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
			else
			{
                await e.ErrorEmbed("Character not found!")
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "getmanga")]
		public async Task GetMangaAsync(EventContext e)
			=> await GetMediaAsync(e, true, MediaFormat.MUSIC, MediaFormat.ONA, MediaFormat.ONE_SHOT, MediaFormat.OVA, MediaFormat.SPECIAL, MediaFormat.TV, MediaFormat.TV_SHORT);

		[Command(Name = "findcharacter")]
		public async Task FindCharacterAsync(EventContext e)
		{
            if (!e.Arguments.Take(out string query))
            {
                return;
            }
            e.Arguments.Take(out int page);

			ISearchResult<ICharacterSearchResult> result = (await anilistClient.SearchCharactersAsync(query, page));

			if (result.Items.Count == 0)
			{
				if (page > result.PageInfo.TotalPages && page != 0)
				{
                    await e.ErrorEmbed($"You've exceeded the total amount of pages available, might want to move back a bit!")
						.ToEmbed().QueueToChannelAsync(e.Channel);
				}
				else
				{
                    await e.ErrorEmbed($"No characters listed containing `{e.Arguments.Pack.TakeAll()}`, try something else!")
						.ToEmbed().QueueToChannelAsync(e.Channel);
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < result.Items.Count; i++)
				sb.AppendLine($"`{result.Items[i].Id.ToString().PadRight(5)}:` {result.Items[i].FirstName} {result.Items[i].LastName}");

            await new EmbedBuilder()
				.SetAuthor($"Search result for `{query}`", "https://anilist.co/img/logo_al.png", "")
				.SetDescription(sb.ToString())
				.SetColor(0, 170, 255)
				.SetFooter($"Page {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.ToEmbed().QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "findmanga")]
		public async Task FindMangaAsync(EventContext e)
		{
            if (!e.Arguments.Take(out string query))
            {
                return;
            }
            e.Arguments.Take(out int page);

			ISearchResult<IMediaSearchResult> result = (await anilistClient.SearchMediaAsync(query, page, e.Channel.IsNsfw, MediaFormat.MUSIC, MediaFormat.ONA, MediaFormat.ONE_SHOT, MediaFormat.OVA, MediaFormat.SPECIAL, MediaFormat.TV, MediaFormat.TV_SHORT));

			if (result.Items.Count == 0)
			{
				if (page > result.PageInfo.TotalPages && page != 0)
				{
                    await e.ErrorEmbed($"You've exceeded the total amount of pages available, might want to move back a bit!")
						.ToEmbed().QueueToChannelAsync(e.Channel);
				}
				else
				{
                    await e.ErrorEmbed($"No characters listed containing `{e.Arguments.Pack.TakeAll()}`, try something else!")
						.ToEmbed().QueueToChannelAsync(e.Channel);
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < result.Items.Count; i++)
				sb.AppendLine($"`{result.Items[i].Id.ToString().PadRight(5)}:` {result.Items[i].DefaultTitle}");

            await new EmbedBuilder()
				.SetAuthor($"Search result for `{query}`", "https://anilist.co/img/logo_al.png", "")
				.SetDescription(sb.ToString())
				.SetColor(0, 170, 255)
				.SetFooter($"Page {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.ToEmbed().QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "findanime")]
		public async Task FindAnimeAsync(EventContext e)
		{
            if (!e.Arguments.Take(out string query))
            {
                return;
            }
            e.Arguments.Take(out int page);

			ISearchResult<IMediaSearchResult> result = (await anilistClient.SearchMediaAsync(query, page, e.Channel.IsNsfw, MediaFormat.MANGA, MediaFormat.NOVEL));

			if (result.Items.Count == 0)
			{
				if (page > result.PageInfo.TotalPages && page != 0)
				{
                    await e.ErrorEmbed($"You've exceeded the total amount of pages available, might want to move back a bit!")
						.ToEmbed().QueueToChannelAsync(e.Channel);
				}
				else
				{
                    await e.ErrorEmbed($"No characters listed containing `{e.Arguments.Pack.TakeAll()}`, try something else!")
						.ToEmbed().QueueToChannelAsync(e.Channel);
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < result.Items.Count; i++)
				sb.AppendLine($"`{result.Items[i].Id.ToString().PadRight(5)}:` {result.Items[i].DefaultTitle}");

            await new EmbedBuilder()
				.SetAuthor($"Search result for `{query}`", "https://anilist.co/img/logo_al.png", "")
				.SetDescription(sb.ToString())
				.SetColor(0, 170, 255)
				.SetFooter($"Page {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.ToEmbed().QueueToChannelAsync(e.Channel);
		}

		private async Task GetMediaAsync(EventContext e, bool manga, params MediaFormat[] format)
		{
            IMedia media = null;
            if (e.Arguments.Take(out int mediaId))
            {
                media = await anilistClient.GetMediaAsync(mediaId);
            }
            else if (e.Arguments.Take(out string arg))
            {
                media = await anilistClient.GetMediaAsync(arg, format);
            }

            if (media != null)
			{
				string description = media.Description;
				if (description.Length > 1024)
				{
					description = new string(description.Take(1020).ToArray()) + "...";
				}

				EmbedBuilder embed = new EmbedBuilder()
					.SetAuthor(media.DefaultTitle, "https://anilist.co/img/logo_al.png", media.Url)
					.SetDescription(media.NativeTitle);

                if (!manga)
                {
                    embed.AddInlineField("Status", media.Status ?? "Unknown")
                    .AddInlineField("Episodes", (media.Episodes ?? 0).ToString());
                }
                else
                {
                    embed.AddInlineField("Volumes", (media.Volumes ?? 0).ToString())
                        .AddInlineField("Chapters", (media.Chapters ?? 0).ToString());
                }

                await embed.AddInlineField("Rating", $"{media.Score ?? 0}/100")
				    .AddInlineField("Genres", string.Join("\n", media.Genres) ?? "None")
			        .AddInlineField("Description", description ?? "None")
				    .SetColor(0, 170, 255)
				    .SetThumbnail(media.CoverImage)
				    .SetFooter("Powered by anilist.co", "")
				    .ToEmbed().QueueToChannelAsync(e.Channel);
			}
			else
			{
                await e.ErrorEmbed("Anime not found!")
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}
	}
}