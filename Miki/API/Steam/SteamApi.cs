using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SteamKit2;
using RestSharp;
using Newtonsoft.Json;

namespace Miki.API.Steam
{

	class SteamApi
	{

		const string baseUrl = "http://api.steampowered.com";

		private string steamKey;

		private List<KeyValue> gameList;
		private DateTime gameListLastUpdate;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="APIKey">SteamAPI Key</param>
		public SteamApi( string _key )
		{
			steamKey = _key;
		}

		public async Task<SteamApiUser> GetSteamUser( string id )
		{
			long steamid;
			bool isVanity = !long.TryParse( id, out steamid );

			if( isVanity )
			{
				steamid = await ResolveVanityURL( id );
			}

			using( dynamic steamUser = WebAPI.GetAsyncInterface( "ISteamUser", steamKey ) )
			{
				KeyValue kvUser = await steamUser.GetPlayerSummaries2( steamids: steamid );
				if( kvUser["players"].Children.Count > 0 )
				{
					return new SteamApiUser( kvUser["players"].Children[0] );
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

		public async Task<string> GetGameName( string appid )
		{
			if( DateTime.Now.Subtract( gameListLastUpdate ).TotalMinutes >= 10 )
			{
				await UpdateInternalGameList();
			}

			KeyValue kvGame = gameList.Find( x => x["appid"].AsString() == appid );
			return kvGame["name"].AsString();
		}

		private async Task UpdateInternalGameList()
		{
			using( dynamic steamApps = WebAPI.GetAsyncInterface( "ISteamApps", steamKey ) )
			{
				KeyValue kvGames = await steamApps.GetappList2();
				gameList = kvGames["applist"]["apps"].Children;
			}
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
