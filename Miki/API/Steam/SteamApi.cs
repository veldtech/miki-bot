using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

using SteamKit2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Miki.Rest;

namespace Miki.API.Steam
{

	class SteamApi
	{
		const string baseUrl = "http://api.steampowered.com";
		const string storeApiUrl = "http://store.steampowered.com/api/";

		private string steamKey;

		private List<KeyValue> gameList;
		private DateTime gameListLastUpdate;

		public SteamApi( string _key )
		{
			steamKey = _key;
		}

		public async Task<SteamUserInfo> GetSteamUser( string id )
		{
			bool isVanity = !long.TryParse( id, out long steamid );

			if( isVanity )
			{
				steamid = await ResolveVanityURL( id );
			}

			using( dynamic steamUser = WebAPI.GetAsyncInterface( "ISteamUser", steamKey ) )
			{
				KeyValue kvUser = await steamUser.GetPlayerSummaries2( steamids: steamid );
				if( kvUser["players"].Children.Count > 0 )
				{
					return new SteamUserInfo( kvUser["players"].Children[0] );
				}
				return null;
			}
		}

		public async Task<long> ResolveVanityURL( string vanityName )
		{
			using( dynamic steamUser = WebAPI.GetAsyncInterface( "ISteamUser", steamKey ) )
			{
				KeyValue kvVanity = await steamUser.ResolveVanityURL( vanityurl: vanityName );

				if( kvVanity["success"].AsInteger() == 1 )
				{
					return kvVanity["steamid"].AsLong();
				} else
				{
					return 0;
				}
			}
		}

		public async Task<string> GetSteamLevel( string steamid )
		{
			using( dynamic playerService = WebAPI.GetAsyncInterface( "IPlayerService", steamKey ) )
			{
				KeyValue kvLevel = await playerService.GetSteamLevel1( steamid: steamid );
				return kvLevel["player_level"].AsString();
			}
		}

		public async Task<SteamGameInfo> GetGameInfo( string appid )
		{
			RestClient client = new RestClient( storeApiUrl );
			return null;

			//RestRequest request = new RestRequest( "appdetails", Method.GET );
			//request.AddParameter( "appids", appid );
			//request.AddParameter( "cc", "UK" );

			//IRestResponse response = await client.GetAsync( request );

			//JObject appDetails = JObject.Parse( response.Content );

			//if( !(bool)appDetails[appid.ToString()]["success"] )
			//{
			//	return null;
			//}

			//JToken appData = appDetails[appid.ToString()]["data"];

			//return new SteamGameInfo( appData );
		}
	}

	[Serializable]
	public class NoUserFoundException : Exception
	{
		public NoUserFoundException() { }
		public NoUserFoundException( string message ) : base( message ) { }
		public NoUserFoundException( string message, Exception inner ) : base( message, inner ) { }
		protected NoUserFoundException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context ) : base( info, context ) { }
	}

}
