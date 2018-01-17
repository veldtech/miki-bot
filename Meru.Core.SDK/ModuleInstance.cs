using IA.SDK.Events;
using IA.SDK.Exceptions;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.SDK
{
    public class Module : IModule
    {
        public string Name { get; set; }

        public bool Enabled { get; set; } = true;
        public bool CanBeDisabled { get; set; } = true;

        public MessageRecievedEventDelegate MessageRecieved { get; set; }
        public UserUpdatedEventDelegate UserUpdated { get; set; }
        public GuildUserEventDelegate UserJoinGuild { get; set; }
        public GuildUserEventDelegate UserLeaveGuild { get; set; }
        public GuildEventDelegate JoinedGuild { get; set; }
        public GuildEventDelegate LeftGuild { get; set; }

        public List<ICommandEvent> Events { get; set; }
        public List<IService> Services { get; set; }

        public bool Nsfw { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private Dictionary<ulong, bool> enabled = new Dictionary<ulong, bool>();

        public Module()
        {
        }

        public Module(string name, bool enabled = true)
        {
            Name = name;
            Enabled = enabled;
        }

        public Module(Action<IModule> info)
        {
            info.Invoke(this);
        }

        public Task Install()
        {
            return Task.CompletedTask;
        }

        public Task Uninstall()
        {
            return Task.CompletedTask;
        }

        public Task<bool> IsEnabled(ulong id)
        {
            throw new AddonRunException();
        }

        public Task InstallAsync(object bot)
        {
            throw new NotImplementedException();
        }

        public Task UninstallAsync(object bot)
        {
            throw new NotImplementedException();
        }

        public Task SetEnabled(ulong id, bool value)
        {
            throw new NotImplementedException();
        }
    }
}