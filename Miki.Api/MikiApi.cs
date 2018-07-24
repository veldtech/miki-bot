using Miki.API.Leaderboards;
using Miki.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API
{
	public class MikiApi
	{
		public static MikiApi Instance => _instance;
		static MikiApi _instance = null;

		RestClient client;

		string token = "";
		string baseUrl = "";

		public const int API_VERSION = 1;

		public MikiApi(string base_url, string token)
		{
			if(_instance == null)
			{
				_instance = this;	
			}

			this.token = token;
			baseUrl = base_url;

			client = new RestClient(baseUrl + $"/{API_VERSION}/" );
			client.SetAuthorization(token);
		}

		/// <summary>
		/// Builds the url to the leaderboards page on the miki website
		/// </summary>
		/// <param name="options">Leaderboards Options Object</param>
		/// <returns>https://miki.ai/leaderboards/{guild_id?}/{type}/{page}</returns>
		public string BuildLeaderboardsUrl(LeaderboardsOptions options)
			=> "https://miki.ai" + BuildLeaderboardsRoute(options);

		/// <summary>
		/// Pulls the leaderboards data from the API
		/// </summary>
		/// <param name="options">Leaderboards Options Object</param>
		public async Task<LeaderboardsObject> GetPagedLeaderboardsAsync(LeaderboardsOptions options)
			=> (await client.GetAsync<LeaderboardsObject>(BuildLeaderboardsRoute(options))).Data;

		private string BuildLeaderboardsRoute(LeaderboardsOptions options)
		{
			StringBuilder sb = new StringBuilder()
				.Append("/leaderboards")
				.Append((options.guildId == 0) ? "" : $"/{options.guildId}")
				.Append($"/{options.type.ToString().ToLower()}");

			if (options.type == LeaderboardsType.COMMANDS && !string.IsNullOrEmpty(options.commandSpecified))
			{
				sb.Append($"/{options.commandSpecified.ToLower()}");
			}

			sb.Append($"/{options.pageNumber}");
			return sb.ToString();
		}
	}
}
