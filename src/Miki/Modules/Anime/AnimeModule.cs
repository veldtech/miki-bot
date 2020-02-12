namespace Miki.Modules.Anime
{
    using Miki.Anilist;
    using Miki.Discord;
    using Miki.Framework;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Miki.Framework.Commands;
    using Miki.Utility;

    [Module("Anime")]
	public class AnimeModule
	{
		private readonly AnilistClient anilistClient = new AnilistClient();

		[Command("getanime")]
		public async Task GetAnimeAsync(IContext e)
			=> await GetMediaAsync(
                e, 
                false, 
                MediaFormat.MANGA, 
                MediaFormat.NOVEL);

        [Command("getcharacter")]
        public async Task GetCharacterAsync(IContext e)
        {
            ICharacter character = null;
            if(e.GetArgumentPack().Take(out int characterId))
            {
                character = await anilistClient.GetCharacterAsync(characterId);
            }
            else
            {
                var args = e.GetArgumentPack().Pack.TakeAll();
                if(!string.IsNullOrWhiteSpace(args))
                {
                    character = await anilistClient.GetCharacterAsync(args);
                }
            }

            if(character == null)
            {
                await e.ErrorEmbed("Character not found!")
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }

            string description = character.Description;
            if(description.Length > 1024)
            {
                description = new string(description.Take(1020).ToArray());
                description = new string(description.Take(description.LastIndexOf(' ')).ToArray())
                              + "...";
            }

            await new EmbedBuilder()
                .SetAuthor(
                    $"{character.FirstName} {character.LastName}",
                    "https://anilist.co/img/logo_al.png", 
                    character.SiteUrl)
                .SetDescription(character.NativeName)
                .AddInlineField("Description", description)
                .SetColor(0, 170, 255)
                .SetThumbnail(character.LargeImageUrl)
                .SetFooter("Powered by anilist.co", "")
                .ToEmbed().QueueAsync(e, e.GetChannel());
        }

        [Command("getmanga")]
		public async Task GetMangaAsync(IContext e)
			=> await GetMediaAsync(
                e, 
                true, 
                MediaFormat.MUSIC, 
                MediaFormat.ONA, 
                MediaFormat.ONE_SHOT, 
                MediaFormat.OVA, 
                MediaFormat.SPECIAL, 
                MediaFormat.TV, 
                MediaFormat.TV_SHORT);

        [Command("findanime")]
        public async Task FindAnimeAsync(IContext e)
        {
            if(!e.GetArgumentPack().Take(out string query))
            {
                return;
            }
            e.GetArgumentPack().Take(out int page);

            ISearchResult<IMediaSearchResult> result = (await anilistClient.SearchMediaAsync(query, page, e.GetChannel().IsNsfw, MediaType.ANIME, MediaFormat.MANGA, MediaFormat.NOVEL));

            if(!result.Items.Any())
            {
                if(page > result.PageInfo.TotalPages && page != 0)
                {
                    await e.ErrorEmbed($"You've exceeded the total amount of pages available, might want to move back a bit!")
                        .ToEmbed().QueueAsync(e, e.GetChannel());
                }
                else
                {
                    await e.ErrorEmbed($"No characters listed containing `{e.GetArgumentPack().Pack.TakeAll()}`, try something else!")
                        .ToEmbed().QueueAsync(e, e.GetChannel());
                }
                return;
            }

            StringBuilder sb = new StringBuilder();

            foreach(var t in result.Items)
            {
                sb.AppendLine(
                    $"`{t.Id.ToString().PadRight(5)}:` {t.DefaultTitle}");
            }

            await new EmbedBuilder()
                .SetAuthor($"Search result for `{query}`", "https://anilist.co/img/logo_al.png", "")
                .SetDescription(sb.ToString())
                .SetColor(0, 170, 255)
                .SetFooter($"Page {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
                .ToEmbed().QueueAsync(e, e.GetChannel());
        }

		[Command("findcharacter")]
		public async Task FindCharacterAsync(IContext e)
		{
            if (!e.GetArgumentPack().Take(out string query))
            {
                return;
            }
            e.GetArgumentPack().Take(out int page);

			ISearchResult<ICharacterSearchResult> result = (await anilistClient.SearchCharactersAsync(query, page));

			if (result.Items.Count == 0)
			{
				if (page > result.PageInfo.TotalPages && page != 0)
				{
                    await e.ErrorEmbed($"You've exceeded the total amount of pages available, might want to move back a bit!")
						.ToEmbed().QueueAsync(e, e.GetChannel());
				}
				else
				{
                    await e.ErrorEmbed($"No characters listed containing `{e.GetArgumentPack().Pack.TakeAll()}`, try something else!")
						.ToEmbed().QueueAsync(e, e.GetChannel());
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

            foreach(var t in result.Items)
            {
                sb.AppendLine($"`{t.Id.ToString().PadRight(5)}:` {t.FirstName} {t.LastName}");
            }

            await new EmbedBuilder()
				.SetAuthor($"Search result for `{query}`", "https://anilist.co/img/logo_al.png", "")
				.SetDescription(sb.ToString())
				.SetColor(0, 170, 255)
				.SetFooter($"Page {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.ToEmbed().QueueAsync(e, e.GetChannel());
		}

		[Command("findmanga")]
		public async Task FindMangaAsync(IContext e)
		{
            if (!e.GetArgumentPack().Take(out string query))
            {
                return;
            }
            e.GetArgumentPack().Take(out int page);

			ISearchResult<IMediaSearchResult> result = (await anilistClient.SearchMediaAsync(query, page, e.GetChannel().IsNsfw, MediaType.MANGA, MediaFormat.MUSIC, MediaFormat.ONA, MediaFormat.ONE_SHOT, MediaFormat.OVA, MediaFormat.SPECIAL, MediaFormat.TV, MediaFormat.TV_SHORT));

			if (result.Items.Count == 0)
			{
				if (page > result.PageInfo.TotalPages && page != 0)
				{
                    await e.ErrorEmbed($"You've exceeded the total amount of pages available, might want to move back a bit!")
						.ToEmbed().QueueAsync(e, e.GetChannel());
				}
				else
				{
                    await e.ErrorEmbed($"No characters listed containing `{e.GetArgumentPack().Pack.TakeAll()}`, try something else!")
						.ToEmbed().QueueAsync(e, e.GetChannel());
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

            foreach(var t in result.Items)
            {
                sb.AppendLine(
                    $"`{t.Id.ToString().PadRight(5)}:` {t.DefaultTitle}");
            }

            await new EmbedBuilder()
				.SetAuthor($"Search result for `{query}`", "https://anilist.co/img/logo_al.png", "")
				.SetDescription(sb.ToString())
				.SetColor(0, 170, 255)
				.SetFooter($"Page {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.ToEmbed().QueueAsync(e, e.GetChannel());
		}

		private async Task GetMediaAsync(IContext e, bool manga, params MediaFormat[] format)
		{
            IMedia media = null;
            if (e.GetArgumentPack().Take(out int mediaId))
            {
                media = await anilistClient.GetMediaAsync(mediaId);
            }
            else
            {
                var args = e.GetArgumentPack().Pack.TakeAll();
                if(!string.IsNullOrWhiteSpace(args))
                {
                    media = await anilistClient.GetMediaAsync(args);
                }
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
				    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
			}
			else
			{
                await e.ErrorEmbed("Anime not found!")
					.ToEmbed()
                    .QueueAsync(e, e.GetChannel());
			}
		}
	}
}