using Miki.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Miki.Models.Objects.Backgrounds
{
	public class BackgroundStore
	{
		public List<Background> Backgrounds { get; private set; } = new List<Background>();

		public BackgroundStore()
		{
			if (File.Exists("./resources/backgrounds.json"))
			{
				Backgrounds = JsonConvert.DeserializeObject<List<Background>>(File.ReadAllText("./resources/backgrounds.json"));
			}
			else
			{
				Log.Warning("No resources for backgrounds were loaded!");	
			}
		}
	}

    public class Background
    {
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("price")]
		public int Price { get; set; }

		[JsonProperty("tags")]
		public List<string> Tags { get; set; }

		public string ImageUrl => $"https://miki-cdn.nyc3.digitaloceanspaces.com/image-profiles/backgrounds/background-{ Id }.png";
    }
}
