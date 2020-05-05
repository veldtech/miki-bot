namespace Miki
{
    using Miki.Bot.Models;
    using Miki.Logging;

    public interface IStartupConfiguration
    {

        string ConnectionString { get; }

        Config Configuration { get; }

        bool IsSelfHosted { get; }

        LogLevel LogLevel { get; }
    }
}