using Miki.Bot.Models;
using Miki.Logging;

namespace Miki
{
    public class StartupConfiguration : IStartupConfiguration
    {
        /// <inheritdoc />
        public bool IsSelfHosted { get; set; }

        /// <inheritdoc />
        public LogLevel LogLevel { get; set; }

        /// <inheritdoc />
        public string ConnectionString { get; set; }

        /// <inheritdoc />
        public Config Configuration { get; set; }
    }
}
