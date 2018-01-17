using IA.SDK.Events;
using System.Collections.Generic;
using System.Linq;

namespace IA.Events
{
    internal class EventContainer
    {
        public Dictionary<string, ICommandEvent> CommandEvents { private set; get; } = new Dictionary<string, ICommandEvent>();
        public Dictionary<string, CommandDoneEvent> CommandDoneEvents { private set; get; } = new Dictionary<string, CommandDoneEvent>();

        public Dictionary<string, IEvent> MentionEvents { private set; get; } = new Dictionary<string, IEvent>();
        public Dictionary<string, IEvent> ContinuousEvents { private set; get; } = new Dictionary<string, IEvent>();

        public Dictionary<string, GuildEvent> JoinServerEvents { private set; get; } = new Dictionary<string, GuildEvent>();
        public Dictionary<string, GuildEvent> LeaveServerEvents { private set; get; } = new Dictionary<string, GuildEvent>();

        /// <summary>
        /// I use this to store internal events.
        /// </summary>
        internal Dictionary<string, Event> InternalEvents { private set; get; } = new Dictionary<string, Event>();

        public IEvent GetEvent(string name)
        {
            if (CommandEvents.ContainsKey(name))
            {
                return CommandEvents[name];
            }
            if (MentionEvents.ContainsKey(name))
            {
                return MentionEvents[name];
            }
            if (ContinuousEvents.ContainsKey(name))
            {
                return ContinuousEvents[name];
            }
            if (JoinServerEvents.ContainsKey(name))
            {
                return JoinServerEvents[name];
            }
            if (LeaveServerEvents.ContainsKey(name))
            {
                return LeaveServerEvents[name];
            }
            return null;
        }

        public Event GetInternalEvent(string name)
        {
            return InternalEvents[name];
        }

        public IEvent[] GetAllEvents()
        {
            List<IEvent> allEvents = new List<IEvent>();
            allEvents.AddRange(CommandEvents.Values);
            allEvents.AddRange(MentionEvents.Values);
            allEvents.AddRange(ContinuousEvents.Values);
            allEvents.AddRange(JoinServerEvents.Values);
            allEvents.AddRange(LeaveServerEvents.Values);
            return allEvents.ToArray();
        }

        public Dictionary<string, IEvent> GetAllEventsDictionary()
        {
            Dictionary<string, IEvent> allEvents = new Dictionary<string, IEvent>();
            CommandEvents.ToList().ForEach(x => allEvents.Add(x.Key, x.Value));
            MentionEvents.ToList().ForEach(x => allEvents.Add(x.Key, x.Value));
            ContinuousEvents.ToList().ForEach(x => allEvents.Add(x.Key, x.Value));
            JoinServerEvents.ToList().ForEach(x => allEvents.Add(x.Key, x.Value));
            LeaveServerEvents.ToList().ForEach(x => allEvents.Add(x.Key, x.Value));
            return allEvents;
        }
    }
}