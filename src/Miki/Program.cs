namespace Miki
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Miki.Bot.Models;
    using Miki.Framework;
    using Miki.Logging;

    public class Program
	{
		private static async Task Main(string[] args)
        {
			// Migrate the database if the program was started with the argument '--migrate' or '-m'.
			if(args.Any(x => x.ToLowerInvariant() == "--migrate" 
                             || x.ToLowerInvariant() == "-m"))
			{
                try
                {
                    using(var context = new MikiDbContextFactory()
                        .CreateDbContext())
                    {
                        await context.Database.MigrateAsync()
                            .ConfigureAwait(false);
                    }
                }
                catch(Exception ex)
                {
                    Log.Error("Failed to migrate the database: " + ex.Message);
                    Log.Debug(ex.ToString());
                    return;
                }
            }

			if (args.Any(x => x.ToLowerInvariant() == "--newconfig" || x.ToLowerInvariant() == "-nc"))
            {
                try
                {
                    var conf = await Config.InsertNewConfigAsync(
                        Environment.GetEnvironmentVariable(Constants.ENV_ConStr));

                    Console.WriteLine($"New Config inserted into database with Id '{conf.Id}'.");
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to generate new config: " + ex.Message);
                    Log.Debug(ex.ToString());
                    return;
                }
            }

            CreateLogger();

            Config c = await Config.GetOrInsertAsync(
                Environment.GetEnvironmentVariable(Constants.ENV_ConStr));

            MessageBucket.AddWorker();

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
                            Environment.GetEnvironmentVariable(Constants.ENV_LogLvl)))
                    {
                        Console.WriteLine(msg);
                    }
                })
                .SetLogHeader((msg) => $"[{msg}]: ")
                .SetTheme(theme)
                .Apply();
        }
	}
}