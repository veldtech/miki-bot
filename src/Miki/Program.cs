namespace Miki
{
    using System;
    using System.Threading.Tasks;
    using Miki.Bot.Models;
    using Miki.Logging;

    public class Program
	{
		private static async Task Main()
        {
            CreateLogger();

            Config c;
            await using(ConfigService service = new ConfigService(
                new MikiDbContextFactory().CreateDbContext()))
            {
                c = await service.GetOrInsertAsync();
            }

            await new MikiBotApp(c)
                .StartAsync();
        }

        private static void CreateLogger()
        {
            var theme = new LogTheme();
            theme.SetColor(
                LogLevel.Information,
                new LogColor
                {
                    Foreground = ConsoleColor.White,
                    Background = 0
                });
            theme.SetColor(
                LogLevel.Error,
                new LogColor
                {
                    Foreground = ConsoleColor.Red,
                    Background = 0
                });
            theme.SetColor(
                LogLevel.Warning,
                new LogColor
                {
                    Foreground = ConsoleColor.Yellow,
                    Background = 0
                });

            new LogBuilder()
                .AddLogEvent((msg, lvl) =>
                {
                    if (lvl >= (LogLevel)Enum.Parse(typeof(LogLevel), 
                            Environment.GetEnvironmentVariable(Constants.EnvLogLevel)))
                    {
                        Console.WriteLine(msg);
                    }
                })
                .SetLogHeader(msg => $"[{msg}]: ")
                .SetTheme(theme)
                .Apply();
        }
	}
}