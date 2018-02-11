using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using SteamKit2;

namespace Miki.API.Steam
{
	public class SteamUserInfo
	{
		#region Steam WebAPI Vars
		protected string steamid;
		protected string communityvisibilitystate;
		protected int profilestate;
		protected string personaname;
		protected int lastlogoff;
		protected int commentpermission;
		protected string profileurl;
		protected string avatar;
		protected string avatarmedium;
		protected string avatarfull;
		protected int personastate;
		protected string realname;
		protected string primaryclanid;
		protected int timecreated;
		protected int personastateflags;
		protected string gameextrainfo;
		protected string gameid;
		protected string loccountrycode;
		#endregion

		public SteamUserInfo( KeyValue kvUser )
		{

			steamid = kvUser["steamid"].AsString();
			communityvisibilitystate = kvUser["communityvisibilitystate"].AsString();
			profilestate = kvUser["profilestate"].AsInteger();
			personaname = kvUser["personaname"].AsString();
			lastlogoff = kvUser["lastlogoff"].AsInteger();
			commentpermission = kvUser["commentpermission"].AsInteger();
			profileurl = kvUser["profileurl"].AsString();
			avatar = kvUser["avatar"].AsString();
			avatarmedium = kvUser["avatarmedium"].AsString();
			avatarfull = kvUser["avatarfull"].AsString();
			personastate = kvUser["personastate"].AsInteger();
			realname = kvUser["realname"].AsString();
			primaryclanid = kvUser["primaryclanid"].AsString();
			timecreated = kvUser["timecreated"].AsInteger();
			personastateflags = kvUser["personastateflags"].AsInteger();
			gameextrainfo = kvUser["gameextrainfo"].AsString();
			gameid = kvUser["gameid"].AsString();
			loccountrycode = kvUser["loccountrycode"].AsString() ?? null;

		}

		public string SteamID
		{
			get
			{
				return steamid;
			}
		}
		public DateTime LastLogOff
		{
			get
			{
				return Utils.UnixToDateTime( lastlogoff );
			}
		}
		public string ProfileURL
		{
			get
			{
				return profileurl;
			}
		}
		public string RealName
		{
			get
			{
				return !string.IsNullOrEmpty( realname ) ? realname : "???";
			}
		}
		public int PersonaState
		{
			get
			{
				return personastate;
			}
		}
		public DateTime TimeCreated
		{
			get
			{
				return Utils.UnixToDateTime( timecreated );
			}
		}
		public string CurrentGameName
		{
			get
			{
				return !string.IsNullOrEmpty( gameextrainfo ) ? gameextrainfo : "???";
			}
		}
		public string CountryCode
		{
			get
			{
				return !string.IsNullOrEmpty( loccountrycode ) ? loccountrycode : "???";
			}
		}

		public string GetUsername()
		{
			return personaname;
		}
		public TimeSpan OfflineSince()
		{
			return DateTime.Now.Subtract( LastLogOff );
		}
		public string GetAvatarURL()
		{
			return avatarfull;
		}
		public string GetStatus()
		{
			switch( PersonaState )
			{
				case 1:
					return "Online";
				case 2:
					return "Busy";
				case 3:
					return "Away";
				case 4:
					return "Snooze";
				case 5:
					return "Looking to Play";
				case 6:
					return "Looking to Trade";
				default:
					return "Offline";
			}
		}
		public bool IsPlayingGame()
		{
			return string.IsNullOrEmpty( gameid ) ? false : true;
		}
		public string GetCurrentGameID()
		{
			return gameid;
		}
		public string GetCountryName()
		{
			CultureInfo cInfo = new CultureInfo( loccountrycode );
			return cInfo.DisplayName;
		}
	}
}
