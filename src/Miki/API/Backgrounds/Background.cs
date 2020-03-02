namespace Miki.API.Backgrounds
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    [DataContract]
	public class Background
	{
		[DataMember(Name = "id")]
		[JsonPropertyName("id")]
		public int Id { get; set; }

        [DataMember(Name = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [DataMember(Name = "price")]
        [JsonPropertyName("price")]
        public int Price { get; set; }

        [DataMember(Name = "tags")]
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

		public string ImageUrl => $"https://cdn.miki.ai/image-profiles/backgrounds/background-{ Id }.png";
	}
}