using System;
using System.Threading.Tasks;
using Miki.Framework;
using Miki.Logging;
using Miki.Services;

namespace Miki
{
    public static class Program
    {
        public static readonly string EnvConStr = "MIKI_CONNSTRING";
        public static readonly string EnvLogLevel = "MIKI_LOGLEVEL";
        public static readonly string EnvSelfHost = "MIKI_SELFHOSTED";

        /// <summary>
        /// Start-up point of the app.
        /// </summary>
        private static async Task Main()
        {
            await using var context = new MikiDbContextFactory().CreateDbContext();
            var config = await new ConfigService(new UnitOfWork(context)).GetOrCreateAnyAsync(null);

            var connectionString = Environment.GetEnvironmentVariable(EnvConStr);
            var selfHost = bool.Parse(Environment.GetEnvironmentVariable(EnvSelfHost) ?? "true");
            var loglevel = (LogLevel)Enum.Parse(
                typeof(LogLevel), Environment.GetEnvironmentVariable(EnvLogLevel) ?? "Information");

            var configuration = new StartupConfiguration
            {
                ConnectionString = connectionString,
                Configuration = config,
                LogLevel = loglevel,
                IsSelfHosted = selfHost,
            };

            await new MikiBotApp(configuration).StartAsync();
        }
    }
}