using IA.SDK.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.SDK.Events
{
    public interface ICommandEvent : IEvent
    {
        string[] Aliases { get; set; }
        Dictionary<string, ProcessCommandDelegate> CommandPool { get; set; }
        int Cooldown { get; set; }

        List<DiscordGuildPermission> GuildPermissions { get; set; }

        ProcessCommandDelegate ProcessCommand { get; set; }

        Task Check(IDiscordMessage e, ICommandHandler c, string identifier = "");

        new ICommandEvent SetName(string name);

        new ICommandEvent SetAccessibility(EventAccessibility accessibility);

        ICommandEvent SetAliases(params string[] aliases);

        ICommandEvent SetCooldown(int seconds);

        ICommandEvent SetPermissions(params DiscordGuildPermission[] permissions);

        ICommandEvent On(string args, ProcessCommandDelegate command);

        ICommandEvent Default(ProcessCommandDelegate command);
    }
}