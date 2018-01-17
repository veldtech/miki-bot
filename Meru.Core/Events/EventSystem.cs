using IA.Events.Attributes;
using IA.Models;
using IA.Models.Context;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace IA.Events
{
    public class EventSystem
    {
        public static EventSystem Instance => _instance;
        private static EventSystem _instance = null;

        public delegate Task ExceptionDelegate(Exception ex, ICommandEvent command, IDiscordMessage message);

        public List<ulong> Developers = new List<ulong>();
        private Dictionary<ulong, OnRegisteredMessage> registeredUsers = new Dictionary<ulong, OnRegisteredMessage>();

        public CommandHandler CommandHandler;
        private List<CommandHandler> commandHandlers = new List<CommandHandler>();

        private ConcurrentDictionary<Tuple<ulong, ulong>, CommandHandler> privateCommandHandlers = new ConcurrentDictionary<Tuple<ulong, ulong>, CommandHandler>();
        private object privateCommandHandlerLock = new object();

        public Dictionary<string, IModule> Modules => CommandHandler.Modules;
        public Dictionary<string, ICommandEvent> Commands => CommandHandler.Commands;

        private List<ulong> ignore = new List<ulong>();

        public Bot bot = null;

        internal EventContainer Events { private set; get; }

        public ExceptionDelegate OnCommandError = async (ex, command, msg) => await Task.Delay(0);

        public EventSystem(Bot bot)
        {
            if (this.bot != null)
            {
                Log.Warning("EventSystem already defined, terminating...");
                return;
            }

            this.bot = bot;
            bot.Events = this;

            Events = new EventContainer();
            CommandHandler = new CommandHandler(this);

            RegisterAttributeCommands();

            bot.MessageReceived += InternalMessageReceived;
            bot.GuildJoin += InternalJoinedGuild;
            bot.GuildLeave += InternalLeftGuild;
        }

        public void AddCommandDoneEvent(Action<CommandDoneEvent> info)
        {
            CommandDoneEvent newEvent = new CommandDoneEvent();
            info.Invoke(newEvent);
            newEvent.eventSystem = this;
            if (newEvent.Aliases.Length > 0)
            {
                foreach (string s in newEvent.Aliases)
                {
                    CommandHandler.aliases.Add(s, newEvent.Name.ToLower());
                }
            }
            Events.CommandDoneEvents.Add(newEvent.Name.ToLower(), newEvent);
        }

        public void Ignore(ulong id)
        {
            ignore.Add(id);
        }

        public void AddContinuousEvent(Action<ContinuousEvent> info)
        {
            ContinuousEvent newEvent = new ContinuousEvent();
            info.Invoke(newEvent);
            newEvent.eventSystem = this;
            Events.ContinuousEvents.Add(newEvent.Name.ToLower(), newEvent);
        }

        public void AddJoinEvent(Action<GuildEvent> info)
        {
            GuildEvent newEvent = new GuildEvent();
            info.Invoke(newEvent);
            newEvent.eventSystem = this;
            if (newEvent.Aliases.Length > 0)
            {
                foreach (string s in newEvent.Aliases)
                {
                    CommandHandler.aliases.Add(s, newEvent.Name.ToLower());
                }
            }
            Events.JoinServerEvents.Add(newEvent.Name.ToLower(), newEvent);
        }

        public void AddLeaveEvent(Action<GuildEvent> info)
        {
            GuildEvent newEvent = new GuildEvent();
            info.Invoke(newEvent);
            newEvent.eventSystem = this;
            if (newEvent.Aliases.Length > 0)
            {
                foreach (string s in newEvent.Aliases)
                {
                    CommandHandler.aliases.Add(s, newEvent.Name.ToLower());
                }
            }
            Events.LeaveServerEvents.Add(newEvent.Name.ToLower(), newEvent);
        }

        public int CommandsUsed()
        {
            int output = 0;
            foreach (ICommandEvent e in CommandHandler.Commands.Values)
            {
                output += e.TimesUsed;
            }
            return output;
        }

        public int CommandsUsed(string eventName)
        {
            return CommandHandler.GetCommandEvent(eventName).TimesUsed;
        }

        internal void DisposeCommandHandler(CommandHandler commandHandler)
        {
            commandHandlers.Remove(commandHandler);
        }

        public bool PrivateCommandHandlerExist(ulong userId, ulong channelId)
        {
            lock (privateCommandHandlerLock)
            {
                return privateCommandHandlers.ContainsKey(new Tuple<ulong, ulong>(userId, channelId));
            }
        }
        internal async Task DisposePrivateCommandHandlerAsync(Tuple<ulong, ulong> key)
        {
            if(!privateCommandHandlers.TryRemove(key, out CommandHandler v))
            {
                await Task.Delay(1000);
                await DisposePrivateCommandHandlerAsync(key);
            }
        }

        internal async Task DisposePrivateCommandHandlerAsync(IDiscordMessage msg)
        {
            await DisposePrivateCommandHandlerAsync(new Tuple<ulong, ulong>(msg.Author.Id, msg.Channel.Id));
        }

        public IEvent GetEvent(string id)
        {
            return Events.GetEvent(id);
        }

        public async Task<SortedDictionary<string, List<string>>> GetEventNamesAsync(IDiscordMessage e)
        {
            SortedDictionary<string, List<string>> moduleEvents = new SortedDictionary<string, List<string>>
            {
                { "MISC", new List<string>() }
            };
            EventAccessibility userEventAccessibility = CommandHandler.GetUserAccessibility(e);

            foreach (ICommandEvent ev in CommandHandler.Commands.Values)
            {
                if (await ev.IsEnabled(e.Channel.Id) && userEventAccessibility >= ev.Accessibility)
                {
                    if (ev.Module != null)
                    {
                        if (!moduleEvents.ContainsKey(ev.Module.Name.ToUpper()))
                        {
                            moduleEvents.Add(ev.Module.Name.ToUpper(), new List<string>());
                        }

                        if (CommandHandler.GetUserAccessibility(e) >= ev.Accessibility)
                        {
                            moduleEvents[ev.Module.Name.ToUpper()].Add(ev.Name);
                        }
                    }
                    else
                    {
                        moduleEvents["MISC"].Add(ev.Name);
                    }
                }
            }

            if (moduleEvents["MISC"].Count == 0)
            {
                moduleEvents.Remove("MISC");
            }

            moduleEvents.OrderBy(i => { return i.Key; });

            foreach (List<string> list in moduleEvents.Values)
            {
                list.Sort((x, y) => x.CompareTo(y));
            }

            return moduleEvents;
        }

        public async Task<string> GetIdentifierAsync(ulong guildId, PrefixInstance prefix)
        {
            using (var context = new IAContext())
            {
                Identifier i = await context.Identifiers.FindAsync(guildId);
                if (i == null)
                {
                    i = context.Identifiers.Add(new Identifier()
					{
						GuildId = guildId.ToDbLong(),
						Value = prefix.DefaultValue
					}).Entity;
                    await context.SaveChangesAsync();
                }
                return i.Value;
            }       
        }

        public PrefixInstance GetPrefixInstance(string defaultPrefix)
        {
            string prefix = defaultPrefix.ToLower();

            if (CommandHandler.Prefixes.ContainsKey(prefix))
            {
                return CommandHandler.Prefixes[prefix];
            }
            return null;
        }

        public IModule GetModuleByName(string name)
        {
            if (CommandHandler.Modules.ContainsKey(name.ToLower()))
            {
                return CommandHandler.Modules[name.ToLower()];
            }
            Log.Warning($"Could not find Module with name '{name}'");
            return null;
        }

        public async Task<string> ListCommandsAsync(IDiscordMessage e)
        {
            SortedDictionary<string, List<string>> moduleEvents = await GetEventNamesAsync(e);

            string output = "";
            foreach (KeyValuePair<string, List<string>> items in moduleEvents)
            {
                output += "**" + items.Key + "**\n";
                for (int i = 0; i < items.Value.Count; i++)
                {
                    output += items.Value[i] + ", ";
                }
                output = output.Remove(output.Length - 2);
                output += "\n\n";
            }
            return output;
        }

        public async Task<IDiscordEmbed> ListCommandsInEmbedAsync(IDiscordMessage e)
        {
            SortedDictionary<string, List<string>> moduleEvents = await GetEventNamesAsync(e);

            IDiscordEmbed embed = new RuntimeEmbed(new Discord.EmbedBuilder());

            foreach (KeyValuePair<string, List<string>> items in moduleEvents)
            {
                for(int i = 0; i < items.Value.Count; i ++)
                {
                    items.Value[i] = $"`{items.Value[i]}`";
                }        

                embed.AddField(items.Key, string.Join(", ",items.Value));
            }
            return embed;
        }

        public void RegisterAttributeCommands()
        {
            Assembly assembly = Assembly.GetEntryAssembly();

            var modules = assembly.GetTypes()
                                  .Where(m => m.GetCustomAttributes<ModuleAttribute>().Count() > 0)
                                  .ToArray();

            foreach (var m in modules)
            {
                RuntimeModule newModule = new RuntimeModule();
                object instance = null;

                try
                {
                    instance = Activator.CreateInstance(Type.GetType(m.AssemblyQualifiedName), newModule);
                }
                catch
                {
                    instance = Activator.CreateInstance(Type.GetType(m.AssemblyQualifiedName));
                }

                newModule.EventSystem = this;

                ModuleAttribute mAttrib = m.GetCustomAttribute<ModuleAttribute>();
                newModule.Name = mAttrib.module.Name.ToLower();
                newModule.Nsfw = mAttrib.module.Nsfw;
                newModule.CanBeDisabled = mAttrib.module.CanBeDisabled;

                var methods = m.GetMethods()
                               .Where(t => t.GetCustomAttributes<CommandAttribute>().Count() > 0)
                               .ToArray();

                foreach (var x in methods)
                {
                    RuntimeCommandEvent newEvent = new RuntimeCommandEvent();
                    CommandAttribute commandAttribute = x.GetCustomAttribute<CommandAttribute>();

                    newEvent = commandAttribute.command;
                    newEvent.ProcessCommand = async (context) => await (Task)x.Invoke(instance, new object[] { context });
                    newEvent.Module = newModule;

                    ICommandEvent foundCommand = newModule.Events.Find(c => c.Name == newEvent.Name);

                    if (foundCommand != null)
                    {
                        if (commandAttribute.on != "")
                        {
                            foundCommand.On(commandAttribute.On, newEvent.ProcessCommand);
                        }
                        else
                        {
                            foundCommand.Default(newEvent.ProcessCommand);
                        }
                    }
                    else
                    {
                        newModule.AddCommand(newEvent);
                    }
                }

                newModule.InstallAsync(bot).GetAwaiter().GetResult();
            }
        }

        internal static void RegisterBot(Bot bot)
        {
            _instance = new EventSystem(bot);
        }

        public PrefixInstance RegisterPrefixInstance(string prefix, bool canBeChanged = true, bool forceExecuteCommands = false)
        {
            PrefixInstance newPrefix = new PrefixInstance(prefix.ToLower(), canBeChanged, forceExecuteCommands);
            CommandHandler.Prefixes.Add(prefix, newPrefix);
            return newPrefix;
        }

        #region events

        internal async Task OnCommandDone(IDiscordMessage e, ICommandEvent commandEvent, bool success = true, float time = 0.0f)
        {
            foreach (CommandDoneEvent ev in Events.CommandDoneEvents.Values)
            {
				try
				{
                    await ev.processEvent(e, commandEvent, success, time);
                }
                catch (Exception ex)
                {
                    Log.ErrorAt($"commanddone@{ev.Name}", ex.Message);
                }
            }
        }

        private async Task OnGuildLeave(IDiscordGuild e)
        {
            foreach (GuildEvent ev in Events.LeaveServerEvents.Values)
            {
                if (await ev.IsEnabled(e.Id))
                {
                    await ev.CheckAsync(e);
                }
            }
        }

        private async Task OnGuildJoin(IDiscordGuild e)
        {
            foreach (GuildEvent ev in Events.JoinServerEvents.Values)
            {
                if (await ev.IsEnabled(e.Id))
                {
                    await ev.CheckAsync(e);
                }
            }
        }

        private async Task OnPrivateMessage(IDiscordMessage arg)
        {
            await Task.CompletedTask;
        }

        private async Task OnMention(IDiscordMessage e)
        {
            foreach (RuntimeCommandEvent ev in Events.MentionEvents.Values)
            {
                await ev.Check(e, null);
            }
        }

        private async Task OnMessageRecieved(IDiscordMessage _message)
        {
            if (_message.Author.IsBot || ignore.Contains(_message.Author.Id))
            {
                return;
            }

            await CommandHandler.CheckAsync(_message);

            foreach (CommandHandler c in commandHandlers)
            {
                if (c.ShouldBeDisposed && c.ShouldDispose())
                {
                    lock (privateCommandHandlerLock)
                    {
                        commandHandlers.Remove(c);
                    }
                }

                await c.CheckAsync(_message);
            }

            Tuple<ulong, ulong> privateKey = new Tuple<ulong, ulong>(_message.Author.Id, _message.Channel.Id);

            if (privateCommandHandlers.ContainsKey(privateKey))
            {
                if (privateCommandHandlers[privateKey].ShouldBeDisposed && privateCommandHandlers[privateKey].ShouldDispose())
                {
                    await DisposePrivateCommandHandlerAsync(_message);
                }
                else
                {
                    await privateCommandHandlers[privateKey].CheckAsync(_message);
                }
            }
        }

        private void AddPrivateCommandHandler(Tuple<ulong, ulong> key, CommandHandler value)
        {
            privateCommandHandlers.AddOrUpdate(key, value,
                (k, existingVal) =>
                {
                    if (value != existingVal)
                    {
                        return existingVal;
                    }
                    return value;
                });
        }

        public void AddPrivateCommandHandler(IDiscordMessage msg, CommandHandler cHandler)
        {
            AddPrivateCommandHandler(new Tuple<ulong, ulong>(msg.Author.Id, msg.Channel.Id), cHandler);
        }

		private async Task InternalMessageReceived(IDiscordMessage message)
		{
			await Task.Yield();
			try
			{
				Task.Run(() => OnMessageRecieved(message));
			}
			catch (Exception e)
			{
				Log.ErrorAt("messagerecieved", e.ToString());
			};
		}
		
        private async Task InternalJoinedGuild(IDiscordGuild g)
        {
            await OnGuildJoin(g);
        }

        private async Task InternalLeftGuild(IDiscordGuild g)
        {
            await OnGuildLeave(g);
        }

        #endregion events
    }

    public delegate void OnRegisteredMessage(IDiscordMessage m);
}