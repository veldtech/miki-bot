namespace Miki
{
    using System.Threading.Tasks;

    public class Program
	{
        /// <summary>
        /// Start-up point of the app.
        /// </summary>
        /// <returns></returns>
		private static async Task Main()
        {
            await new MikiBotApp()
                .StartAsync();
        }
    }
}