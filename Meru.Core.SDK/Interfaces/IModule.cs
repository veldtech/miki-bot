using IA.SDK.Events;
using IA.SDK.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.SDK
{
    public interface IModule
    {
        string Name { get; set; }

        bool Enabled { get; set; }
        bool CanBeDisabled { get; set; }
        bool Nsfw { get; set; }

        MessageRecievedEventDelegate MessageRecieved { get; set; }
        UserUpdatedEventDelegate UserUpdated { get; set; }
        GuildUserEventDelegate UserJoinGuild { get; set; }
        GuildUserEventDelegate UserLeaveGuild { get; set; }
        GuildEventDelegate JoinedGuild { get; set; }
        GuildEventDelegate LeftGuild { get; set; }

        List<ICommandEvent> Events { get; set; }
        List<IService> Services { get; set; }

        Task<bool> IsEnabled(ulong id);

        Task SetEnabled(ulong id, bool value);

        Task InstallAsync(object bot);

        Task UninstallAsync(object bot);
    }
}