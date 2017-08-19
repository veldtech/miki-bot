using IA.SDK;
using IA.SDK.Events;
using IA.Events.Attributes;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Miki.Modules
{
	[Module( Name = "Osu" )]
	public class OsuModule
	{
		[Command( Name = "osu" )]
		public async Task SendOsuSignatureAsync( EventContext e )
		{
			using( WebClient webClient = new WebClient() )
			{
				byte[] data = webClient.DownloadData( "http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + e.arguments + "&countryrank" );

				using( MemoryStream mem = new MemoryStream( data ) )
				{
					await e.Channel.SendFileAsync( mem, $"sig.png" );
				}
			}
		}

		[Command( Name = "ctb" )]
		public async Task SendCatchTheBeatSignatureAsync( EventContext e )
		{
			using( WebClient webClient = new WebClient() )
			{
				byte[] data = webClient.DownloadData( "http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + e.arguments + "&mode=2&countryrank" );

				using( MemoryStream mem = new MemoryStream( data ) )
				{
					await e.Channel.SendFileAsync( mem, $"{e.arguments}.png" );
				}
			}
		}

		[Command( Name = "mania" )]
		public async Task SendManiaSignatureAsync( EventContext e )
		{
			using( WebClient webClient = new WebClient() )
			{
				byte[] data = webClient.DownloadData( "http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + e.arguments + "&mode=3&countryrank" );

				using( MemoryStream mem = new MemoryStream( data ) )
				{
					await e.Channel.SendFileAsync( mem, $"sig.png" );
				}
			}
		}

		[Command( Name = "taiko" )]
		public async Task SendTaikoSignatureAsync( EventContext e )
		{
			using( WebClient webClient = new WebClient() )
			{
				byte[] data = webClient.DownloadData( "http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + e.arguments + "&mode=1&countryrank" );

				using( MemoryStream mem = new MemoryStream( data ) )
				{
					await e.Channel.SendFileAsync( mem, $"sig.png" );
				}
			}
		}
	}
}
