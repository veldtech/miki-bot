using Miki.Framework.Events.Attributes;
using Miki.Common.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Miki.Rest;
using Newtonsoft.Json;
using System.Linq;
using Miki.Anilist;
using Miki.GraphQL;
using Miki.Common.Interfaces;

namespace Miki.Core.Modules.Anime
{
	[Module("Anime")]
	public class AnimeModule
	{
		AnilistClient anilistClient = new AnilistClient();

		[Command(Name = "getanime")]
		public async Task GetAnimeAsync(EventContext e)
			=> await GetMediaAsync(e, false, MediaFormat.MANGA, MediaFormat.NOVEL);

		[Command(Name = "getcharacter")]
		public async Task GetCharacterAsync(EventContext e)
		{
			ICharacter character = null;

			if (int.TryParse(e.arguments, out int result))
			{
				character = await anilistClient.GetCharacterAsync(result);
			}
			else
			{
				character = await anilistClient.GetCharacterAsync(e.arguments);
			}

			if (character != null)
			{
				string description = character.Description;
				if (description.Length > 1024)
				{
					description = new string(description.Take(1020).ToArray());
					description = new string(description.Take(description.LastIndexOf(' ')).ToArray()) + "...";
				}

				Utils.Embed.SetAuthor($"{character.FirstName} {character.LastName}", "https://anilist.co/img/logo_al.png", character.SiteUrl)
					.SetDescription(character.NativeName)
					.AddInlineField("Description", description)
					.SetColor(0, 170, 255)
					.SetThumbnailUrl(character.LargeImageUrl)
					.SetFooter("Powered by anilist.co", "")
					.QueueToChannel(e.Channel);
			}
			else
			{
				e.ErrorEmbed("Character not found!")
					.QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "getmanga")]
		public async Task GetMangaAsync(EventContext e)
			=> await GetMediaAsync(e, true, MediaFormat.MUSIC, MediaFormat.ONA, MediaFormat.ONE_SHOT, MediaFormat.OVA, MediaFormat.SPECIAL, MediaFormat.TV, MediaFormat.TV_SHORT);

		[Command(Name = "findcharacter")]
		public async Task FindCharacterAsync(EventContext e)
		{
			List<string> args = e.arguments.Split(' ').ToList();
			int page = 0;

			if(int.TryParse(args.Last(), out int r))
			{
				args.RemoveAt(args.Count - 1);
				page = r;
			}

			string searchQuery = string.Join(' ', args);

			ISearchResult<ICharacterSearchResult> result = (await anilistClient.SearchCharactersAsync(searchQuery, page));

			if(result.Items.Count == 0)
			{
				if (page > result.PageInfo.TotalPages && page != 0)
				{
					e.ErrorEmbed($"You've exceeded the total amount of pages available, might want to move back a bit!")
						.QueueToChannel(e.Channel);
				}
				else
				{
					e.ErrorEmbed($"No characters listed containing `{e.arguments}`, try something else!")
						.QueueToChannel(e.Channel);
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < result.Items.Count; i++)
				sb.AppendLine($"`{result.Items[i].Id.ToString().PadRight(5)}:` {result.Items[i].FirstName} {result.Items[i].LastName}");

			Utils.Embed.SetAuthor($"Search result for `{searchQuery}`", "https://anilist.co/img/logo_al.png", "")
				.SetDescription(sb.ToString())
				.SetColor(0, 170, 255)
				.SetFooter($"Page {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.QueueToChannel(e.Channel);
		}

		[Command(Name = "findmanga")]
		public async Task FindMangaAsync(EventContext e)
		{
			List<string> args = e.arguments.Split(' ').ToList();
			int page = 0;

			if (int.TryParse(args.Last(), out int r))
			{
				args.RemoveAt(args.Count - 1);
				page = r;
			}

			string searchQuery = string.Join(' ', args);

			ISearchResult<IMediaSearchResult> result = (await anilistClient.SearchMediaAsync(searchQuery, page, e.Channel.Nsfw, MediaFormat.MUSIC, MediaFormat.ONA, MediaFormat.ONE_SHOT, MediaFormat.OVA, MediaFormat.SPECIAL, MediaFormat.TV, MediaFormat.TV_SHORT));

			if (result.Items.Count == 0)
			{
				if (page > result.PageInfo.TotalPages && page != 0)
				{
					e.ErrorEmbed($"You've exceeded the total amount of pages available, might want to move back a bit!")
						.QueueToChannel(e.Channel);
				}
				else
				{
					e.ErrorEmbed($"No characters listed containing `{e.arguments}`, try something else!")
						.QueueToChannel(e.Channel);
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < result.Items.Count; i++)
				sb.AppendLine($"`{result.Items[i].Id.ToString().PadRight(5)}:` {result.Items[i].DefaultTitle}");

			Utils.Embed.SetAuthor($"Search result for `{searchQuery}`", "https://anilist.co/img/logo_al.png", "")
				.SetDescription(sb.ToString())
				.SetColor(0, 170, 255)
				.SetFooter($"Page {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.QueueToChannel(e.Channel);
		}

		[Command(Name = "findanime")]
		public async Task FindAnimeAsync(EventContext e)
		{
			List<string> args = e.arguments.Split(' ').ToList();
			int page = 0;

			if (int.TryParse(args.Last(), out int r))
			{
				args.RemoveAt(args.Count - 1);
				page = r;
			}

			string searchQuery = string.Join(' ', args);

			ISearchResult<IMediaSearchResult> result = (await anilistClient.SearchMediaAsync(searchQuery, page, e.Channel.Nsfw, MediaFormat.MANGA, MediaFormat.NOVEL));

			if (result.Items.Count == 0)
			{
				if (page > result.PageInfo.TotalPages && page != 0)
				{
					e.ErrorEmbed($"You've exceeded the total amount of pages available, might want to move back a bit!")
						.QueueToChannel(e.Channel);
				}
				else
				{
					e.ErrorEmbed($"No characters listed containing `{e.arguments}`, try something else!")
						.QueueToChannel(e.Channel);
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < result.Items.Count; i++)
				sb.AppendLine($"`{result.Items[i].Id.ToString().PadRight(5)}:` {result.Items[i].DefaultTitle}");

			Utils.Embed.SetAuthor($"Search result for `{searchQuery}`", "https://anilist.co/img/logo_al.png", "")
				.SetDescription(sb.ToString())
				.SetColor(0, 170, 255)
				.SetFooter($"Page {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.QueueToChannel(e.Channel);
		}

		private async Task GetMediaAsync(EventContext e, bool manga, params MediaFormat[] format)
		{
			IMedia media = null;

			if (int.TryParse(e.arguments, out int result))
			{
				media = await anilistClient.GetMediaAsync(result);
			}
			else
			{
				string filter = "search: $p0, format_not_in: $p1";
				List<GraphQLParameter> parameters = new List<GraphQLParameter>
				{
					new GraphQLParameter(e.arguments),
					new GraphQLParameter(format, "[MediaFormat]")
				};

				if (!e.Channel.Nsfw)
				{
					filter += ", isAdult: $p2";
					parameters.Add(new GraphQLParameter(false, "Boolean"));
				}

				media = await anilistClient.GetMediaAsync(filter, parameters.ToArray());
			}

			if (media != null)
			{
				string description = media.Description;
				if (description.Length > 1024)
				{
					description = new string(description.Take(1020).ToArray());
					description = new string(description.Take(description.LastIndexOf(' ')).ToArray()) + "...";
				}

				IDiscordEmbed embed = Utils.Embed.SetAuthor(media.DefaultTitle, "https://anilist.co/img/logo_al.png", media.Url)
					.SetDescription(media.NativeTitle);

				if (!manga)
					embed.AddInlineField("Status", media.Status ?? "Unknown")
					.AddInlineField("Episodes", media.Episodes ?? 0);
				else
					embed.AddInlineField("Volumes", media.Volumes ?? 0)
						.AddInlineField("Chapters", media.Chapters ?? 0);

					embed.AddInlineField("Rating", $"{media.Score ?? 0}/100")
					.AddInlineField("Genres", string.Join("\n", media.Genres) ?? "None")
				.AddInlineField("Description", description ?? "None")
					.SetColor(0, 170, 255)
					.SetThumbnailUrl(media.CoverImage)
					.SetFooter("Powered by anilist.co", "")
					.QueueToChannel(e.Channel);
			}
			else
			{
				e.ErrorEmbed("Anime not found!")
					.QueueToChannel(e.Channel);
			}
		}
	}
}
