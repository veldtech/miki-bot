using IA.Events.Attributes;
using IA.SDK.Events;
using MalApi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Rest;

namespace Miki.Core.Modules.Anime
{
	class AnimeCharacter
	{
		AnimeName Name;
		string Description;
		AnimeImage Image;
	}

	class AnimeName
	{
		string FirstName;
		string LastName;
		string Native;
	}

	class AnimeImage
	{
		string Large;
		string Medium;
	}

	[Module("Anime")]
    public class AnimeModule
    {
		[Command(Name = "findcharacter")]
		public async Task GetCharacterAsync(EventContext e)
		{
			RestClient client = new RestClient("https://graphql.anilist.co");
			RestResponse<AnimeCharacter> response = await client.PostAsync<AnimeCharacter>(
				"{ Character(search: \"" + e.arguments + "\") { name { first last native } description image { large medium } } }");

			Console.WriteLine("");

		}
    }
}
