using IA.Models;
using IA.Models.Context;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.Events
{
    public class Event : IEvent
    {
        public string Name { get; set; } = "$command-not-named";

        public EventAccessibility Accessibility { get; set; } = EventAccessibility.PUBLIC;
        public EventMetadata Metadata { get; set; } = new EventMetadata();

        public bool OverridableByDefaultPrefix { get; set; } = false;
        public bool CanBeDisabled { get; set; } = true;
        public bool DefaultEnabled { get; set; } = true;

        public IModule Module { get; set; }

        public int TimesUsed { get; set; } = 0;

        internal EventSystem eventSystem;

        public ConcurrentDictionary<ulong, bool> cache = new ConcurrentDictionary<ulong, bool>();
        protected Dictionary<ulong, EventCooldownObject> lastTimeUsed = new Dictionary<ulong, EventCooldownObject>();

        public Event()
        {
        }
        public Event(IEvent eventObject)
        {
            Name = eventObject.Name;
            Accessibility = eventObject.Accessibility;
            OverridableByDefaultPrefix = eventObject.OverridableByDefaultPrefix;
            CanBeDisabled = eventObject.CanBeDisabled;
            DefaultEnabled = eventObject.DefaultEnabled;
            Module = eventObject.Module;
            TimesUsed = eventObject.TimesUsed;
        }
        public Event(Action<Event> info)
        {
            info.Invoke(this);
        }

        public async Task SetEnabled(ulong channelId, bool enabled)
        {
            using (var context = new IAContext())
            {
                CommandState state = await context.CommandStates.FindAsync(Name, channelId.ToDbLong());
                if (state == null)
                {
                    state = context.CommandStates.Add(new CommandState() { ChannelId = channelId.ToDbLong(), CommandName = Name, State = DefaultEnabled }).Entity;
                }
                state.State = enabled;

                cache.AddOrUpdate(channelId, enabled, (x, y) =>
                {
                    return enabled;
                });

                await context.SaveChangesAsync();
            }
        }

        public async Task SetEnabledAll(IDiscordGuild guildId, bool enabled)
        {
            List<IDiscordMessageChannel> channels = await guildId.GetChannels();
            foreach (IDiscordMessageChannel c in channels)
            {
                await SetEnabled(c.Id, enabled);
            }
        }

        public async Task<bool> IsEnabled(ulong id)
        {
            if (Module != null)
            {
                if (!await Module.IsEnabled(id)) return false;
            }

            if (cache.TryGetValue(id, out bool value))
            {
				return value;
            }
            else
            {
                CommandState state = null;

				using (var context = new IAContext())
				{
					long guildId = id.ToDbLong();
					state = await context.CommandStates.FindAsync(Name, guildId);
				}
                return cache.GetOrAdd(id, state?.State ?? DefaultEnabled);
            }
        }

        public IEvent SetName(string name)
        {
            Name = name;
            return this;
        }

        public IEvent SetAccessibility(EventAccessibility accessibility)
        {
            Accessibility = accessibility;
            return this;
        }
    }

    public class EventCooldownObject
    {
        DateTime lastTimeUsed;
        DateTime prevLastTimeUsed;

        DateTime canBeUsedWhen;

        int coolDown = 1;

        public EventCooldownObject(int Cooldown)
        {
            lastTimeUsed = DateTime.Now;
            coolDown = Cooldown;
        }

        public void Tick()
        {
            prevLastTimeUsed = lastTimeUsed;
            lastTimeUsed = DateTime.Now;

            double s = Math.Max(0, coolDown - (lastTimeUsed - prevLastTimeUsed).TotalSeconds);

            canBeUsedWhen = DateTime.Now.AddSeconds(s);
        }

        public bool CanBeUsed()
        {
            return canBeUsedWhen <= DateTime.Now;
        }
    }
}