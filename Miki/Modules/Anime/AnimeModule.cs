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

namespace Miki.Core.Modules.Anime
{
	[Module("Anime")]
	public class AnimeModule
	{
		AnilistClient anilistClient = new AnilistClient();

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
	}
}
