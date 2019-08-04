using Newtonsoft.Json;

namespace Miki.Modules.Overwatch.Objects
{
	public class OverwatchUserResponse
	{
		[JsonProperty("us")]
		public OverwatchRegion America { get; set; }

		[JsonProperty("any")]
		public OverwatchRegion Any { get; set; }

		[JsonProperty("eu")]
		public OverwatchRegion Europe { get; set; }

		[JsonProperty("kr")]
		public OverwatchRegion Korea { get; set; }

		[JsonProperty("_request")]
		public RequestMetadata Request { get; set; }
	}
}