using IA.SDK.Events;
using IA.SDK.Interfaces;
using System.Threading.Tasks;

namespace IA.SDK
{
    public enum EventAccessibility
    {
        PUBLIC,
        ADMINONLY,
        DEVELOPERONLY
    }

    public enum EventRange
    {
        USER,
        CHANNEL,
        SERVER
    }

    public delegate Task ProcessServerCommand(IDiscordGuild guild);

    public delegate Task ProcessCommandDelegate(EventContext context);

    public delegate bool CheckCommandDelegate(IDiscordMessage message, string command, string[] allAliases);
}