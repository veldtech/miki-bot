using Miki.API.Leaderboards;
using Miki.Rest;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API
{
	public class MikiApi : IDisposable
	{
		public static MikiApi Instance { get; private set; } = null;

		private RestClient _client;

		private readonly string _token = "";
		private readonly string _baseUrl = "";

		public MikiApi(string base_url, string token)
		{
			if (Instance == null)
			{
				Instance = this;
			}

			this._token = token;
			_baseUrl = base_url;

			_client = new RestClient(_baseUrl);
			_client.SetAuthorization(token);
		}

		/// <summary>
		/// Builds the url to the leaderboards page on the miki website
		/// </summary>
		/// <param name="options">Leaderboards Options Object</param>
		/// <returns>https://miki.ai/leaderboards/{type}/{guild_id?}</returns>
		public string BuildLeaderboardsUrl(LeaderboardsOptions options)
			=> "https://miki.ai" + BuildLeaderboardsRoute(options);

		/// <summary>
		/// Pulls the leaderboards data from the API
		/// </summary>
		/// <param name="options">Leaderboards Options Object</param>
		public async Task<LeaderboardsObject> GetPagedLeaderboardsAsync(LeaderboardsOptions options)
			=> (await _client.GetAsync<LeaderboardsObject>(BuildLeaderboardsRoute(options))).Data;

		private string BuildLeaderboardsRoute(LeaderboardsOptions options)
		{
			StringBuilder sb = new StringBuilder()
				.Append("/leaderboards")
				.Append($"/{options.Type.ToString().ToLower()}");

			if (options.GuildId.HasValue)
			{
				sb.Append($"/{options.GuildId}");
			}

			QueryString qs = new QueryString();

			qs.Add("amount", options.Amount);
			qs.Add("offset", options.Offset);

			return sb.ToString() + qs.Query;
		}

		public void Dispose()
		{
			_client.Dispose();
		}
	}
}