using Miki.Anilist;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Events;
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

		[Command("getanime","geefanime")]
		public async Task GetAnimeAsync(IContext e)
			=> await GetMediaAsync(e, false, MediaFormat.MANGA, MediaFormat.NOVEL);

		[Command("getcharacter","geefcaracter")]
		public async Task GetCharacterAsync(IContext e)
		{
			ICharacter character = null;
			if (e.GetArgumentPack().Take(out int characterId))
			{
				character = await anilistClient.GetCharacterAsync(characterId);
			}
			else if (e.GetArgumentPack().Take(out string arg))
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
					.AddInlineField("Discriptie", description)
					.SetColor(0, 170, 255)
					.SetThumbnail(character.LargeImageUrl)
					.SetFooter("Powered by anilist.co", "")
					.ToEmbed().QueueAsync(e.GetChannel());
			}
			else
			{
                await e.ErrorEmbed("Caracter niet gevonden!")
					.ToEmbed().QueueAsync(e.GetChannel());
			}
		}

		[Command("getmanga","geefmanga")]
		public async Task GetMangaAsync(IContext e)
			=> await GetMediaAsync(e, true, MediaFormat.MUSIC, MediaFormat.ONA, MediaFormat.ONE_SHOT, MediaFormat.OVA, MediaFormat.SPECIAL, MediaFormat.TV, MediaFormat.TV_SHORT);

		[Command("findcharacter","zoekcaracter")]
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
                    await e.ErrorEmbed($"Je hebt het totaal aantal beschikbare pagina's overschreden! Je wilt misschien een beetje terug gaan!")
						.ToEmbed().QueueAsync(e.GetChannel());
				}
				else
				{
                    await e.ErrorEmbed($"Geen caracters gevonden met `{e.GetArgumentPack().Pack.TakeAll()}`, probeer iets anders!")
						.ToEmbed().QueueAsync(e.GetChannel());
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < result.Items.Count; i++)
				sb.AppendLine($"`{result.Items[i].Id.ToString().PadRight(5)}:` {result.Items[i].FirstName} {result.Items[i].LastName}");

            await new EmbedBuilder()
				.SetAuthor($"Zoek resultaaten voor `{query}`", "https://anilist.co/img/logo_al.png", "")
				.SetDescription(sb.ToString())
				.SetColor(0, 170, 255)
				.SetFooter($"Pagina {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.ToEmbed().QueueAsync(e.GetChannel());
		}

		[Command("findmanga","zoekmanga")]
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
                    await e.ErrorEmbed($"Je hebt het totaal aantal pagina's berijkt! Je zou misschien een beetje terug gaan!")
						.ToEmbed().QueueAsync(e.GetChannel());
				}
				else
				{
                    await e.ErrorEmbed($"Geen caracters gevonden met `{e.GetArgumentPack().Pack.TakeAll()}`, probeer iets anders!")
						.ToEmbed().QueueAsync(e.GetChannel());
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < result.Items.Count; i++)
				sb.AppendLine($"`{result.Items[i].Id.ToString().PadRight(5)}:` {result.Items[i].DefaultTitle}");

            await new EmbedBuilder()
				.SetAuthor($"Zoek resultaat voor `{query}`", "https://anilist.co/img/logo_al.png", "")
				.SetDescription(sb.ToString())
				.SetColor(0, 170, 255)
				.SetFooter($"Pagina {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.ToEmbed().QueueAsync(e.GetChannel());
		}

		[Command("findanime","zoekanime")]
		public async Task FindAnimeAsync(IContext e)
		{
            if (!e.GetArgumentPack().Take(out string query))
            {
                return;
            }
            e.GetArgumentPack().Take(out int page);

			ISearchResult<IMediaSearchResult> result = (await anilistClient.SearchMediaAsync(query, page, e.GetChannel().IsNsfw, MediaType.ANIME, MediaFormat.MANGA, MediaFormat.NOVEL));

			if (result.Items.Count == 0)
			{
				if (page > result.PageInfo.TotalPages && page != 0)
				{
                    await e.ErrorEmbed($"Je hebt het totaal aantal pagina's berijkt! Je zou misschien een beetje terug gaan!")
						.ToEmbed().QueueAsync(e.GetChannel());
				}
				else
				{
                    await e.ErrorEmbed($"Geen caracters gevonden met `{e.GetArgumentPack().Pack.TakeAll()}`, probeer iets anders!")
						.ToEmbed().QueueAsync(e.GetChannel());
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < result.Items.Count; i++)
				sb.AppendLine($"`{result.Items[i].Id.ToString().PadRight(5)}:` {result.Items[i].DefaultTitle}");

            await new EmbedBuilder()
				.SetAuthor($"Zoek resultaat voor `{query}`", "https://anilist.co/img/logo_al.png", "")
				.SetDescription(sb.ToString())
				.SetColor(0, 170, 255)
				.SetFooter($"Pagina {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.ToEmbed().QueueAsync(e.GetChannel());
		}

		private async Task GetMediaAsync(IContext e, bool manga, params MediaFormat[] format)
		{
            IMedia media = null;
            if (e.GetArgumentPack().Take(out int mediaId))
            {
                media = await anilistClient.GetMediaAsync(mediaId);
            }
            else if (e.GetArgumentPack().Take(out string arg))
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
                    embed.AddInlineField("Status", media.Status ?? "Onbekend")
                    .AddInlineField("Afleveringen", (media.Episodes ?? 0).ToString());
                }
                else
                {
                    embed.AddInlineField("Volumes", (media.Volumes ?? 0).ToString())
                        .AddInlineField("Chapters", (media.Chapters ?? 0).ToString());
                }

                await embed.AddInlineField("Rating", $"{media.Score ?? 0}/100")
				    .AddInlineField("Genres", string.Join("\n", media.Genres) ?? "None")
			        .AddInlineField("Beschrijving", description ?? "None")
				    .SetColor(0, 170, 255)
				    .SetThumbnail(media.CoverImage)
				    .SetFooter("Powered by anilist.co", "")
				    .ToEmbed()
                    .QueueAsync(e.GetChannel());
			}
			else
			{
                await e.ErrorEmbed("Anime niet gevonden!")
					.ToEmbed()
                    .QueueAsync(e.GetChannel());
			}
		}
	}
}