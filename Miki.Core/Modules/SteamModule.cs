using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Extensions;
using IA.SDK.Interfaces;
using Miki.Languages;
using Miki.API.Steam;

using SteamKit2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Miki.Modules
{

	[Module( "Steam" )]
	public class SteamModule
	{
		SteamApi steam = new SteamApi(Global.config.SteamAPIKey);

		private string steamAuthorIcon = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/83/Steam_icon_logo.svg/1024px-Steam_icon_logo.svg.png";
		private string steamAuthorName = "Steam";

		public SteamModule( RuntimeModule module )
		{
			if(string.IsNullOrWhiteSpace(Global.config.SteamAPIKey))
			{
				Log.Warning( "SteamAPI key has not been set, steam module disabled." );
				module.Enabled = false;
			}
		}

		[Command( Name = "steam" )]
		public async Task SteamRequestHandler( EventContext context )
		{
			IDiscordEmbed embed = Utils.Embed;
			embed.SetAuthor( steamAuthorName, steamAuthorIcon, "" );
			embed.Description = "Steam API at your fingertips.\nYou can find a list of commands by typing `" + ">" + "help steam`!";
			await embed.QueueToChannel( context.Channel );
		}

		[Command( Name = "steamhelp" )] // TODO: Kill this command. >help steam should be used instead.
		public async Task SteamHelpAsync( EventContext context )
		{
			IDiscordEmbed embed = Utils.Embed;
			embed.SetAuthor( steamAuthorName, steamAuthorIcon, "" );
			embed.Description = "Steam API at your fingertips.";
			embed.AddInlineField( "Commands", "`>steam` \n`>steam user <vanity/steam64>`" );
			await embed.QueueToChannel( context.Channel );
		}

		// TODO: Comply to privacy rules.
		// TODO: Show profile with link, eg; http://steamcommunity.com/id/<NameHere>/ or http://steamcommunity.com/profile/<SteamIDHere>/
		[Command( Name = "steamuser" )]
		public async Task SteamUserAsync( EventContext context )
		{
			DateTime requestStart = DateTime.Now;
			string[] args = context.arguments.Split( ' ' );

			IDiscordEmbed embed = Utils.Embed;
			embed.SetAuthor( "Steam Profile", steamAuthorIcon, "" );

			SteamUserInfo user = await steam.GetSteamUser( args[0] );

			if( user == null )
			{
				embed = Utils.ErrorEmbed( context, "No user was found!" );
				await embed.QueueToChannel( context.Channel );
				return;
			}

			string userLevel = await steam.GetSteamLevel( user.SteamID );

			embed.SetThumbnailUrl( user.GetAvatarURL() );

			/* Current Game & Embed Colour */
			if( user.IsPlayingGame() )
			{
				if( user.CurrentGameName != "???" )
					embed.SetDescription( "Currently playing " + user.CurrentGameName );
				else
					embed.SetDescription( "Currently in-game" );
				embed.Color = Color.GetColor( IAColor.GREEN );
			} else if( user.PersonaState != 0 )
			{
				embed.Color = Color.GetColor( IAColor.BLUE );
			}

			/* Name & ID */
			embed.AddInlineField( "Name", user.GetUsername() );
			embed.AddInlineField( "ID", user.SteamID );

			/* Real Name & Country */
			embed.AddInlineField( "Real Name", user.RealName );
			embed.AddInlineField( "Country", ( user.CountryCode != "???" ? ":flag_" + user.CountryCode.ToLower() + ": " : "" ) + user.CountryCode );

			/* Profile Link */
			embed.AddField( "Link", user.ProfileURL );

			/* Created & Status */
			embed.AddInlineField( "Created", String.Format( "{0:MMMM d, yyyy}", user.TimeCreated ) );
			if( user.GetStatus() == "Offline" )
			{
				embed.AddInlineField( "Offline Since", ToTimeString( user.OfflineSince() ) );
			} else
			{
				embed.AddInlineField( "Status", user.GetStatus() );
			}

			/* Level */
			embed.AddInlineField( "Level", userLevel );

			embed.SetFooter( "Request took in " + Math.Round( ( DateTime.Now - requestStart ).TotalMilliseconds ) + "ms", "" );
			await embed.QueueToChannel( context.Channel );
		}

		public async Task SteamGameAsync( EventContext context )
		{
			DateTime requestStart = DateTime.Now;
			string[] args = context.arguments.Split( ' ' );

			IDiscordEmbed embed = Utils.Embed;
			embed.SetAuthor( "Steam Game", steamAuthorIcon, "" );

			SteamGameInfo gameInfo = await steam.GetGameInfo( args[0] );

			embed.SetDescription( gameInfo.Name );
			embed.SetThumbnailUrl( gameInfo.HeaderImage );

			embed.SetFooter( "Request took in " + Math.Round( ( DateTime.Now - requestStart ).TotalMilliseconds ) + "ms", "" );
			await embed.QueueToChannel( context.Channel );
		}

		private string ToTimeString( TimeSpan time )
		{
			if( Math.Floor( time.TotalDays ) > 0 )
			{
				return Math.Floor( time.TotalDays ) + " day" + ( ( time.TotalDays > 1 ) ? "s" : "" );
			}

			return ( ( Math.Floor( time.TotalDays ) > 0 ) ? ( Math.Floor( time.TotalDays ) + " day" + ( ( time.TotalDays > 1 ) ? "s" : "" ) + ", " ) : "" ) +
			  ( ( time.Hours > 0 ) ? ( time.Hours + " hour" + ( ( time.Hours > 1 ) ? "s" : "" ) + ", " ) : "" ) +
			  ( ( time.Minutes > 0 ) ? ( time.Minutes + " minute" + ( ( time.Minutes != 1 ) ? "s" : "" ) ) : "" ) + ".\n";
		}
	}
}
