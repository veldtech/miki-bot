using IA.SDK.Events;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.SDK
{
    public class AddonInstance : IAddonInstance
    {
        public string Name { get; set; }
        public List<IModule> Modules { get; set; } = new List<IModule>();

        public AddonInstance()
        {
            Modules = new List<IModule>();
        }

        public void CreateModule(Action<IModule> x) => throw new NotImplementedException();

        public virtual async Task QueryAsync(string text, QueryOutput output, params object[] parameters) => throw new NotImplementedException();

        public virtual ICommandEvent GetCommandEvent(string args) => throw new NotImplementedException();

        public virtual Task<string> ListCommands(IDiscordMessage e) => throw new NotImplementedException();

        public virtual Task<IDiscordEmbed> ListCommandsInEmbed(IDiscordMessage e) => throw new NotImplementedException();

        public virtual EventAccessibility GetUserAccessibility(IDiscordMessage e) => throw new NotImplementedException();

        public virtual IEnumerable<Module> GetModules() => throw new NotImplementedException();

        public virtual Task<string> GetIdentifierAsync(ulong id) => throw new NotImplementedException();

        public virtual Task SetIdentifierAsync(IDiscordGuild guild, string defaultPrefix, string newPrefix) => throw new NotImplementedException();

        public virtual string GetBotVersion() => throw new NotImplementedException();

        List<IModule> IAddonInstance.GetModules() => throw new NotImplementedException();

        public int GetGuildCount() => throw new NotImplementedException();

        public ulong GetBotId(IDiscordGuild guild) => throw new NotImplementedException();
    }
}