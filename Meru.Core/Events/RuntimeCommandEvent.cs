using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace IA.Events
{
    public class RuntimeCommandEvent : Event, ICommandEvent
    {
        public Dictionary<string, ProcessCommandDelegate> CommandPool { get; set; } = new Dictionary<string, ProcessCommandDelegate>();
        public int Cooldown { get; set; } = 3;

        public List<DiscordGuildPermission> GuildPermissions { get; set; } = new List<DiscordGuildPermission>();
        public string[] Aliases { get; set; } = new string[] { };

        public ProcessCommandDelegate ProcessCommand { get; set; } = async (context) => await Task.Delay(0);

        public RuntimeCommandEvent()
        {
        }

        public RuntimeCommandEvent(string name)
        {
            Name = name;
        }

        public RuntimeCommandEvent(ICommandEvent commandEvent) : base(commandEvent)
        {
            Aliases = commandEvent.Aliases;
            Cooldown = commandEvent.Cooldown;
            GuildPermissions = commandEvent?.GuildPermissions;
            ProcessCommand = commandEvent?.ProcessCommand;
            CommandPool = commandEvent?.CommandPool;
        }

        public RuntimeCommandEvent(Action<RuntimeCommandEvent> info)
        {
            info.Invoke(this);
        }

        public async Task Check(IDiscordMessage e, ICommandHandler c, string identifier = "")
        {
            string command = e.Content.Substring(identifier.Length).Split(' ')[0];
            string args = "";
            List<string> allAliases = new List<string>();
            List<string> arguments = new List<string>();

            if (e.Content.Split(' ').Length > 1)
            {
                args = e.Content.Substring(e.Content.Split(' ')[0].Length + 1);
                arguments.AddRange(args.Split(' '));
				arguments = arguments
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.ToList();
            }

            if (Module != null)
            {
                if (Module.Nsfw && !e.Channel.Nsfw)
                {
                    return;
                }
            }

            if (Aliases != null)
            {
                allAliases.AddRange(Aliases);
                allAliases.Add(Name);
            }

            if (IsOnCooldown(e.Author.Id))
            {
                Log.WarningAt(Name, " is on cooldown");
                return;
            }

            if (GuildPermissions.Count > 0)
            {
                foreach (DiscordGuildPermission g in GuildPermissions)
                {
                    if (!e.Author.HasPermissions(e.Channel, g))
                    {
                        await e.Channel.SendMessage($"Please give me the guild permission `{g}` to use this command.");
                        return;
                    }
                }
            }

            ProcessCommandDelegate targetCommand = ProcessCommand;

            if (arguments.Count > 0)
            {
                if (CommandPool.ContainsKey(arguments[0]))
                {
                    targetCommand = CommandPool[arguments[0]];
                    args = args.Substring((arguments[0].Length == args.Length) ? arguments[0].Length : arguments[0].Length + 1);
                }
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            EventContext context = new EventContext();
            context.commandHandler = c;
            context.arguments = args;
            context.message = e;    

            if (await TryProcessCommand(targetCommand, context))
            {
                await eventSystem.OnCommandDone(e, this, true, sw.ElapsedMilliseconds);
                TimesUsed++;
                Log.Message($"{Name} called by {e.Author.Username}#{e.Author.Discriminator} [{e.Author.Id}] from {e.Guild.Name} in {sw.ElapsedMilliseconds}ms");
            }
			else
			{
				await eventSystem.OnCommandDone(e, this, false, sw.ElapsedMilliseconds);
			}
			sw.Stop();
		}

		private bool IsOnCooldown(ulong id)
        {
            if (lastTimeUsed.ContainsKey(id))
            {
                if (lastTimeUsed[id].CanBeUsed())
                {
                    lastTimeUsed[id].Tick();
                    return false;
                }
                return true;
            }
            else
            {
                lastTimeUsed.Add(id, new EventCooldownObject(Cooldown));
                return false;
            }
        }

        private async Task<bool> TryProcessCommand(ProcessCommandDelegate cmd, EventContext context)
        {
            bool success = false;
            try
            {
                await cmd(context);
                success = true;
            }
            catch (Exception ex)
            {
                Log.ErrorAt(Name, ex.Message + "\n" + ex.StackTrace);
                await eventSystem.OnCommandError(ex, this, context.message);
            }
            return success;
        }

        public ICommandEvent SetCooldown(int seconds)
        {
            Cooldown = seconds;
            return this;
        }

        public ICommandEvent SetPermissions(params DiscordGuildPermission[] permissions)
        {
            GuildPermissions.AddRange(permissions);
            return this;
        }

        public ICommandEvent On(string args, ProcessCommandDelegate command)
        {
            CommandPool.Add(args, command);
            return this;
        }

        public ICommandEvent Default(ProcessCommandDelegate command)
        {
            ProcessCommand = command;
            return this;
        }

        new public ICommandEvent SetName(string name)
        {
            Name = name;
            return this;
        }

        new public ICommandEvent SetAccessibility(EventAccessibility accessibility)
        {
            Accessibility = accessibility;
            return this;
        }

        public ICommandEvent SetAliases(params string[] aliases)
        {
            Aliases = aliases;
            return this;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}