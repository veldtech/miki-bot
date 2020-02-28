namespace Miki
{
    using System.Threading.Tasks;

    public class Program
    {
        /// <summary>
        /// Start-up point of the app.
        /// </summary>
        private static Task Main() => new MikiBotApp().StartAsync();
    }
}