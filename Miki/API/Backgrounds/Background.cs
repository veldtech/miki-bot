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
			Backgrounds = JsonConvert.DeserializeObject<List<Background>>(File.ReadAllText("./resources/backgrounds.json"));
		}
	}

    public class Background
    {
		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("price")]
		public int Price { get; set; }

		[JsonProperty("tags")]
		public List<string> Tags { get; set; }

		public string GetImageUrl => $"https://miki-cdn.nyc3.digitaloceanspaces.com/avatars/{ Id }.png";
    }
}
