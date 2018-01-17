using Discord;
using IA.Events;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IA.SDK
{
    internal class RuntimeAddonInstance : IAddonInstance
    {
        public string Name { get; set; } = "";
        public List<IModule> Modules { get; set; } = new List<IModule>();
        private Bot bot = null;

        public RuntimeAddonInstance()
        {
        }

        public RuntimeAddonInstance(IAddonInstance i, Bot bot)
        {
            Name = i.Name;
            Modules = i.Modules;
            this.bot = bot;
        }

        public string GetBotVersion()
        {
            return Bot.instance.Version;
        }

        public async Task<string> ListCommands(IDiscordMessage e)
        {
            return await Bot.instance.Events.ListCommandsAsync(e);
        }

        public async Task<IDiscordEmbed> ListCommandsInEmbed(IDiscordMessage e)
        {
            return await Bot.instance.Events.ListCommandsInEmbedAsync(e);
        }

        public ICommandEvent GetCommandEvent(string args)
        {
            return Bot.instance.Events.CommandHandler.GetCommandEvent(args);
        }

        public void CreateModule(Action<IModule> module)
        {
            IModule m = new RuntimeModule(module);
            Modules.Add(m);
        }

        public EventAccessibility GetUserAccessibility(IDiscordMessage message)
        {
            return Bot.instance.Events.CommandHandler.GetUserAccessibility(message);
        }

        public async Task<string> GetIdentifierAsync(ulong serverid)
        {
            return await Bot.instance.Events.GetIdentifierAsync(serverid, PrefixInstance.Default);
        }

        public List<IModule> GetModules()
        {
            return Bot.instance.Events.Modules.Values.ToList();
        }

        public async Task SetIdentifierAsync(IDiscordGuild guild, string defaultPrefix, string newPrefix)
        {
            await Bot.instance.Events
                .GetPrefixInstance(defaultPrefix)
                .ChangeForGuildAsync(guild.Id, newPrefix);
        }

        public int GetGuildCount()
        {
            return Bot.instance.Client.Guilds.Count;
        }

        public ulong GetBotId(IDiscordGuild guild)
        {
            Bot b = Bot.instance;
            ulong id = b.Client.GetShardFor((guild as IProxy<IGuild>).ToNativeObject()).CurrentUser.Id;

            return id;
        }
    }
}