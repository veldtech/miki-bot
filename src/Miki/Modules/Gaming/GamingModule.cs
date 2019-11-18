namespace Miki.Modules.Gaming
{
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Framework;
    using Framework.Commands.Attributes;

    [Module("Gaming")]
	internal class GamingModule
    {
        private const string baseUrl = "http://lemmmy.pw/osusig/sig.php";

        [Command("osu")]
		public async Task SendOsuSignatureAsync(IContext e)
		{
            e.GetArgumentPack().Take(out string username);

            using WebClient webClient = new WebClient();
            byte[] data = webClient.DownloadData(
                $"{baseUrl}?colour=pink&uname={username}&countryrank");

            await using MemoryStream mem = new MemoryStream(data);
            await e.GetChannel().SendFileAsync(mem, $"sig.png");
        }

		[Command("ctb")]
		public async Task SendCatchTheBeatSignatureAsync(IContext e)
		{
            e.GetArgumentPack().Take(out string username);

            using WebClient webClient = new WebClient();
            byte[] data = webClient.DownloadData(
                $"{baseUrl}?colour=pink&uname={username}&mode=2&countryrank");

            await using MemoryStream mem = new MemoryStream(data);
            await e.GetChannel().SendFileAsync(mem, $"{username}.png");
        }

		[Command("mania")]
		public async Task SendManiaSignatureAsync(IContext e)
		{
            e.GetArgumentPack().Take(out string username);

            using WebClient webClient = new WebClient();
            byte[] data = webClient.DownloadData(
                $"{baseUrl}?colour=pink&uname={username}&mode=3&countryrank");

            await using MemoryStream mem = new MemoryStream(data);
            await e.GetChannel().SendFileAsync(mem, $"sig.png");
        }

		[Command("taiko")]
		public async Task SendTaikoSignatureAsync(IContext e)
		{
            e.GetArgumentPack().Take(out string username);

            using WebClient webClient = new WebClient();
            byte[] data = webClient.DownloadData(
                $"{baseUrl}?colour=pink&uname={username}&mode=1&countryrank");

            await using MemoryStream mem = new MemoryStream(data);
            await e.GetChannel().SendFileAsync(mem, $"sig.png");
        }
    }
}