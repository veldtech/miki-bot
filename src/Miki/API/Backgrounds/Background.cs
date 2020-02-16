namespace Miki.API.Backgrounds
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
	public class Background
	{
		[DataMember(Name = "id")]
		public int Id { get; set; }

        [DataMember(Name = "name")]
		public string Name { get; set; }

        [DataMember(Name = "price")]
		public int Price { get; set; }

        [DataMember(Name = "tags")]
		public List<string> Tags { get; set; }

		public string ImageUrl => $"https://cdn.miki.ai/image-profiles/backgrounds/background-{ Id }.png";
	}
}