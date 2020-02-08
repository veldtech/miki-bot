namespace Miki.UrbanDictionary.Objects
{
    using System.Runtime.Serialization;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    [DataContract]
    internal class UrbanDictionaryResponse : IUrbanDictionaryResponse
	{
		[DataMember(Name = "tags")]
		public IReadOnlyList<string> Tags { get; set; }

		[DataMember(Name = "result_type")]
		public string ResultType { get; set; }

        [DataMember(Name = "list")]
		public IReadOnlyList<UrbanDictionaryEntry> List { get; set; }

		[DataMember(Name = "sounds")]
		public IReadOnlyList<string> Sounds { get; set; }
	}
}