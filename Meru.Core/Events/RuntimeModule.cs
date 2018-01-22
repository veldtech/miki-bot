using IA.Models;
using IA.Models.Context;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IA.Events
{
    public class RuntimeModule : IModule
    {
        public string Name { get; set; } = "";
        public bool Nsfw { get; set; } = false;
        public bool Enabled { get; set; } = true;
        public bool CanBeDisabled { get; set; } = true;

        public Mutex threadLock;

        public MessageRecievedEventDelegate MessageRecieved { get; set; } = null;
        public UserUpdatedEventDelegate UserUpdated { get; set; } = null;
        public GuildUserEventDelegate UserJoinGuild { get; set; } = null;
        public GuildUserEventDelegate UserLeaveGuild { get; set; } = null;
        public GuildEventDelegate JoinedGuild { get; set; } = null;
        public GuildEventDelegate LeftGuild { get; set; } = null;

        public List<ICommandEvent> Events { get; set; } = new List<ICommandEvent>();
        public List<IService> Services { get; set; } = new List<IService>();

        private ConcurrentDictionary<ulong, bool> cache = new ConcurrentDictionary<ulong, bool>();

        internal EventSystem EventSystem;

        public string SqlName
        {
            get
            {
                return "module:" + Name;
            }
        }

        private bool isInstalled = false;

        internal RuntimeModule()
        {
        }

        public RuntimeModule(string name, bool enabled = true)
        {
            Name = name;
            Enabled = enabled;
        }

        public RuntimeModule(IModule info)
        {
            Name = info.Name;
            Enabled = info.Enabled;
            CanBeDisabled = info.CanBeDisabled;
            Events = info.Events;
        }

        public RuntimeModule(Action<IModule> info)
        {
            info.Invoke(this);
        }

        public async Task InstallAsync(object bot)
        {
            Bot b = (Bot)bot;
            Name = Name.ToLower();

            if (MessageRecieved != null)
            {
                b.MessageReceived += Module_MessageReceived;
            }

            if (UserUpdated != null)
            {
                b.UserUpdated += Module_UserUpdated;
            }

            if (UserJoinGuild != null)
            {
                b.UserJoin += Module_UserJoined;
            }

            if (UserLeaveGuild != null)
            {
                b.UserLeft += Module_UserLeft;
            }

            if (JoinedGuild != null)
            {
                b.GuildJoin += Module_JoinedGuild;
            }

            if (LeftGuild != null)
            {
                b.GuildLeave += Module_LeftGuild;
            }

            EventSystem = b.Events;

            b.Events.CommandHandler.Modules.Add(Name, this);

            foreach (ICommandEvent e in Events)
            {
                RuntimeCommandEvent ev = new RuntimeCommandEvent(e)
                {
                    eventSystem = b.Events,
                    Module = this
                };
                EventSystem.CommandHandler.AddCommand(ev);
            }

            isInstalled = true;

            await Task.CompletedTask;
        }

        public RuntimeModule AddCommand(ICommandEvent command)
        {
            Events.Add(command);
            return this;
        }

        public async Task UninstallAsync(object bot)
        {
            Bot b = (Bot)bot;

            if (!isInstalled)
            {
                return;
            }

            b.Events.Modules.Remove(Name);

            b.Events.CommandHandler.AddModule(this);

            if (MessageRecieved != null)
            {
                b.MessageReceived -= Module_MessageReceived;
            }

            if (UserUpdated != null)
            {
                b.UserUpdated -= Module_UserUpdated;
            }

            if (UserJoinGuild != null)
            {
                b.UserJoin -= Module_UserJoined;
            }

            if (UserLeaveGuild != null)
            {
                b.UserLeft -= Module_UserLeft;
            }

            if (JoinedGuild != null)
            {
                b.GuildJoin -= Module_JoinedGuild;
            }

            if (LeftGuild != null)
            {
                b.GuildLeave -= Module_LeftGuild;
            }

            isInstalled = false;
            await Task.CompletedTask;
        }

        private async Task Module_JoinedGuild(IDiscordGuild arg)
        {
            if (await IsEnabled(arg.Id))
            {
				try
				{
					await JoinedGuild(arg);
				}
				catch { }
            }
        }

        public RuntimeModule SetNsfw(bool val)
        {
            Nsfw = val;
            return this;
        }

        private async Task Module_LeftGuild(IDiscordGuild arg)
        {
			if (await IsEnabled(arg.Id))
			{
				try
				{
					await LeftGuild(arg);
				}
				catch { }
			}
		}

        private async Task Module_UserJoined(IDiscordUser arg)
        {
            if (await IsEnabled(arg.Guild.Id))
            {
				try
				{
					await UserJoinGuild(arg.Guild, arg);
				}
				catch(Exception e)
				{
					Log.ErrorAt("userjoin", e.Message + "\n" + e.StackTrace);
				}
            }
        }

        private async Task Module_UserLeft(IDiscordUser arg)
        {
            if (await IsEnabled(arg.Guild.Id))
            {
				try
				{
					await UserLeaveGuild(arg.Guild, arg);
				}
				catch { }
            }
        }

        private async Task Module_UserUpdated(IDiscordUser arg1, IDiscordUser arg2)
        {
            if (arg1.Guild != null)
            {
                if (await IsEnabled(arg1.Guild.Id))
                {
					try {
						await UserUpdated(arg1, arg2);
					}
					catch { }
                }
            }
        }

        private async Task Module_MessageReceived(IDiscordMessage message)
        {
            if (await IsEnabled(message.Guild.Id))
            {
				await MessageRecieved(message);
            }
        }

        public async Task SetEnabled(ulong serverId, bool enabled)
        {
            using (var context = new IAContext())
            {
                ModuleState state = await context.ModuleStates.FindAsync(SqlName, serverId.ToDbLong());
                if (state == null)
                {
                    state = context.ModuleStates.Add(new ModuleState() { ChannelId = serverId.ToDbLong(), ModuleName = SqlName, State = Enabled }).Entity;
                }
                state.State = enabled;

                cache.AddOrUpdate(serverId, enabled, (x, y) =>
                {
                    return enabled;
                });

                await context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsEnabled(ulong id)
        {
            ModuleState state = null;

            if (cache.ContainsKey(id))
            {
                return cache.GetOrAdd(id, Enabled);
            }
            else
            {
				using (var context = new IAContext())
				{
					long guildId = id.ToDbLong();
					state = await context.ModuleStates.FindAsync(SqlName, guildId);
				}

                if (state == null)
                {
                    return cache.GetOrAdd(id, Enabled);
                }

                return cache.GetOrAdd(id, state.State);
            }
        }
    }
}