using Miki.Framework.Events.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Miki.Rest;
using Newtonsoft.Json;
using System.Linq;
using Miki.Anilist;
using Miki.GraphQL;
using Miki.Common;
using Miki.Framework.Events;
using Miki.Framework.Extension;
using Discord;

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

			ArgObject arg = e.Arguments.First();

			int? characterId = arg.AsInt();

			if (characterId != null)
			{
				character = await anilistClient.GetCharacterAsync(characterId.Value);
			}
			else
			{
				character = await anilistClient.GetCharacterAsync(arg.Argument);
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
					.WithDescription(character.NativeName)
					.AddInlineField("Description", description)
					.WithColor(0, 170, 255)
					.WithThumbnailUrl(character.LargeImageUrl)
					.WithFooter("Powered by anilist.co", "")
					.Build().QueueToChannel(e.Channel);
			}
			else
			{
				e.ErrorEmbed("Character not found!")
					.Build().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "getmanga")]
		public async Task GetMangaAsync(EventContext e)
			=> await GetMediaAsync(e, true, MediaFormat.MUSIC, MediaFormat.ONA, MediaFormat.ONE_SHOT, MediaFormat.OVA, MediaFormat.SPECIAL, MediaFormat.TV, MediaFormat.TV_SHORT);

		[Command(Name = "findcharacter")]
		public async Task FindCharacterAsync(EventContext e)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg == null)
				return;

			int page = 0;

			if (e.Arguments.LastOrDefault()?.AsInt() != null)
			{
				page = e.Arguments.LastOrDefault().AsInt().Value;
			}

			arg = arg.TakeUntilEnd((page != 0) ? 1 : 0);
			string searchQuery = arg.Argument;

			arg = arg.Next();

			ISearchResult<ICharacterSearchResult> result = (await anilistClient.SearchCharactersAsync(searchQuery, page));

			if(result.Items.Count == 0)
			{
				if (page > result.PageInfo.TotalPages && page != 0)
				{
					e.ErrorEmbed($"You've exceeded the total amount of pages available, might want to move back a bit!")
						.Build().QueueToChannel(e.Channel);
				}
				else
				{
					e.ErrorEmbed($"No characters listed containing `{e.Arguments.ToString()}`, try something else!")
						.Build().QueueToChannel(e.Channel);
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < result.Items.Count; i++)
				sb.AppendLine($"`{result.Items[i].Id.ToString().PadRight(5)}:` {result.Items[i].FirstName} {result.Items[i].LastName}");

			Utils.Embed.SetAuthor($"Search result for `{searchQuery}`", "https://anilist.co/img/logo_al.png", "")
				.WithDescription(sb.ToString())
				.WithColor(0, 170, 255)
				.WithFooter($"Page {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.Build().QueueToChannel(e.Channel);
		}

		[Command(Name = "findmanga")]
		public async Task FindMangaAsync(EventContext e)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg == null)
				return;

			int page = 0;

			if (e.Arguments.LastOrDefault()?.AsInt() != null)
			{
				page = e.Arguments.LastOrDefault().AsInt().Value;
			}

			arg = arg.TakeUntilEnd((page != 0) ? 1 : 0);
			string searchQuery = arg.Argument;

			arg = arg.Next();

			ISearchResult<IMediaSearchResult> result = (await anilistClient.SearchMediaAsync(searchQuery, page, (e.Channel as ITextChannel).IsNsfw, MediaFormat.MUSIC, MediaFormat.ONA, MediaFormat.ONE_SHOT, MediaFormat.OVA, MediaFormat.SPECIAL, MediaFormat.TV, MediaFormat.TV_SHORT));

			if (result.Items.Count == 0)
			{
				if (page > result.PageInfo.TotalPages && page != 0)
				{
					e.ErrorEmbed($"You've exceeded the total amount of pages available, might want to move back a bit!")
						.Build().QueueToChannel(e.Channel);
				}
				else
				{
					e.ErrorEmbed($"No characters listed containing `{e.Arguments.ToString()}`, try something else!")
						.Build().QueueToChannel(e.Channel);
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < result.Items.Count; i++)
				sb.AppendLine($"`{result.Items[i].Id.ToString().PadRight(5)}:` {result.Items[i].DefaultTitle}");

			Utils.Embed.SetAuthor($"Search result for `{searchQuery}`", "https://anilist.co/img/logo_al.png", "")
				.WithDescription(sb.ToString())
				.WithColor(0, 170, 255)
				.WithFooter($"Page {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.Build().QueueToChannel(e.Channel);
		}

		[Command(Name = "findanime")]
		public async Task FindAnimeAsync(EventContext e)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg == null)
				return;

			int page = 0;

			page = e.Arguments.LastOrDefault().AsInt() ?? 0;
		
			arg = arg.TakeUntilEnd((page != 0) ? 1 : 0);
			string searchQuery = arg.Argument;

			arg = arg.Next();


			ISearchResult<IMediaSearchResult> result = (await anilistClient.SearchMediaAsync(searchQuery, page, (e.Channel as ITextChannel).IsNsfw, MediaFormat.MANGA, MediaFormat.NOVEL));

			if (result.Items.Count == 0)
			{
				if (page > result.PageInfo.TotalPages && page != 0)
				{
					e.ErrorEmbed($"You've exceeded the total amount of pages available, might want to move back a bit!")
						.Build().QueueToChannel(e.Channel);
				}
				else
				{
					e.ErrorEmbed($"No characters listed containing `{e.Arguments.ToString()}`, try something else!")
						.Build().QueueToChannel(e.Channel);
				}
				return;
			}

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < result.Items.Count; i++)
				sb.AppendLine($"`{result.Items[i].Id.ToString().PadRight(5)}:` {result.Items[i].DefaultTitle}");

			Utils.Embed.SetAuthor($"Search result for `{searchQuery}`", "https://anilist.co/img/logo_al.png", "")
				.WithDescription(sb.ToString())
				.WithColor(0, 170, 255)
				.WithFooter($"Page {result.PageInfo.CurrentPage} of {result.PageInfo.TotalPages} | Powered by anilist.co", "")
				.Build().QueueToChannel(e.Channel);
		}

		private async Task GetMediaAsync(EventContext e, bool manga, params MediaFormat[] format)
		{
			IMedia media = null;

			ArgObject arg = e.Arguments.First();

			int? mediaId = arg.AsInt();

			if (mediaId != null)
			{
				media = await anilistClient.GetMediaAsync(mediaId.Value);
			}
			else
			{
				string filter = "search: $p0, format_not_in: $p1";
				List<GraphQLParameter> parameters = new List<GraphQLParameter>
				{
					new GraphQLParameter(arg.Argument),
					new GraphQLParameter(format, "[MediaFormat]")
				};

				if (!(e.Channel as ITextChannel).IsNsfw)
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

				EmbedBuilder embed = Utils.Embed.SetAuthor(media.DefaultTitle, "https://anilist.co/img/logo_al.png", media.Url)
					.WithDescription(media.NativeTitle);

				if (!manga)
					embed.AddInlineField("Status", media.Status ?? "Unknown")
					.AddInlineField("Episodes", media.Episodes ?? 0);
				else
					embed.AddInlineField("Volumes", media.Volumes ?? 0)
						.AddInlineField("Chapters", media.Chapters ?? 0);

					embed.AddInlineField("Rating", $"{media.Score ?? 0}/100")
					.AddInlineField("Genres", string.Join("\n", media.Genres) ?? "None")
				.AddInlineField("Description", description ?? "None")
					.WithColor(0, 170, 255)
					.WithThumbnailUrl(media.CoverImage)
					.WithFooter("Powered by anilist.co", "")
					.Build().QueueToChannel(e.Channel);
			}
			else
			{
				e.ErrorEmbed("Anime not found!")
					.Build().QueueToChannel(e.Channel);
			}
		}
	}
}
