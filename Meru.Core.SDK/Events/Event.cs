using IA.SDK.Exceptions;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.SDK.Events
{
    // TODO: Find a way to remove this whole class.
    public class Event : IEvent
    {
        public string Name { get; set; } = "$command-not-named";
        public string[] Aliases { get; set; } = new string[0];

        public EventAccessibility Accessibility { get; set; } = EventAccessibility.PUBLIC;
        public EventMetadata Metadata { get; set; } = new EventMetadata();

        public bool OverridableByDefaultPrefix { get; set; } = false;
        public bool CanBeDisabled { get; set; } = true;
        public bool DefaultEnabled { get; set; } = true;

        public IModule Module { get; set; }

        public int TimesUsed { get; set; } = 0;

        public Event()
        {
        }

        public Event(Action<Event> info)
        {
            info.Invoke(this);
        }

        public async Task<bool> IsEnabled(ulong id)
        {
            throw new AddonRunException();
        }

        public Task SetEnabled(ulong id, bool value)
        {
            throw new AddonRunException();
        }

        public Task SetEnabledAll(IDiscordGuild guild, bool value)
        {
            throw new AddonRunException();
        }

        public IEvent SetName(string name)
        {
            throw new NotImplementedException();
        }

        public IEvent SetAccessibility(EventAccessibility accessibility)
        {
            throw new NotImplementedException();
        }

        public IEvent SetAliases(params string[] aliases)
        {
            throw new NotImplementedException();
        }
    }

    public class Metadata
    {
        public string description = "description not set for this object!";
    }

    public class EventMetadata : Metadata
    {
        public string errorMessage = "Something went wrong!";

        public List<string> usage = new List<string>();

        public EventMetadata()
        {
        }

        public EventMetadata(string description, string error, params string[] usage)
        {
            this.description = description;
            errorMessage = error;
            this.usage.AddRange(usage);
        }
    }
}