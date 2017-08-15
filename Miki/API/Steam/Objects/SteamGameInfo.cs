using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Miki.API.Steam
{
	class SteamGameInfo
	{
		#region Steam WebAPI Vars
		protected string type;
		protected string name;
		protected int steam_appid;
		protected int required_age;
		protected bool is_free;
		protected string detailed_description;
		protected string about_the_game;
		protected string short_description;
		protected string supported_languages;
		protected string reviews;
		protected string header_image;
		protected string website;

		protected Dictionary<string, string> pc_requirements;
		protected Dictionary<string, string> mac_requirements;
		protected Dictionary<string, string> linux_requirements;

		protected string[] developers;
		protected string[] publishers;

		private Dictionary<string, string> price_overview;
		#endregion

		public SteamGameInfo( JToken data )
		{
			name = (string)data["name"];
			header_image = (string)data["header_image"];
		}

		public string Name
		{
			get 
			{
				return name;
			}
		}

		public string HeaderImage
		{
			get
			{
				return header_image;
			}
		}
		

	}
}
