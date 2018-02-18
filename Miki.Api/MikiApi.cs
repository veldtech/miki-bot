using Miki.API.Leaderboards;
using Miki.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API
{
	class MikiApi
	{
		public static MikiApi Instance => _instance;
		static MikiApi _instance = null;

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
		}

		public async Task<LeaderboardsObject> GetPagedLeaderboardsAsync(LeaderboardsOptions options)
		{
			StringBuilder sb = Utils.CreateBaseRoute()
				.Append("/leaderboards")
				.Append((options.guildId != 0) ? "/local" : "/global")
				.Append("/")
				.Append(options.type.ToString().ToLower());

			if(options.type == LeaderboardsType.COMMANDS && !string.IsNullOrEmpty(options.commandSpecified))
			{
				sb.Append("/")
				  .Append(options.commandSpecified.ToLower());
			}

			sb.Append("/")
			  .Append(options.pageNumber);

			RestClient client = new RestClient(baseUrl + sb.ToString() + Utils.AddToken(token));
			var response = await client.GetAsync<LeaderboardsObject>("");
			return response.Data;
		}
	}
}
