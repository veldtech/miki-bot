namespace Miki.Services.Scheduling
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public class WorkPayload 
    {
        /// <summary>
        /// Task attached to this worker.
        /// </summary>
        [DataMember(Name = "parent_id", Order = 1)]
        public string TaskName { get; set; }

        /// <summary>
        /// Unique identifier to seperate this from it's siblings.
        /// </summary>
        [DataMember(Name = "id", Order = 2)]
        public string Id { get; set; }

        /// <summary>
        /// Time when this task has started. This time is required to be UTC.
        /// </summary>
        [DataMember(Name = "start_time", Order = 3)]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Time for when to call.
        /// </summary>
        [DataMember(Name = "duration", Order = 4)]
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Json string of the payload sent as the response.,,
        /// </summary>
        [DataMember(Name = "payload", Order = 5)]
        public string PayloadJson { get; set; }

        /// <summary>
        /// Whether the task repeats itself after execution.
        /// </summary>
        [DataMember(Name = "payload", Order = 6)]
        public bool IsRepeating { get; set; }

        /// <summary>
        /// The time in epoch when the 
        /// </summary>
        [DataMember(Name = "time_epoch", Order = 7)]
        public Epoch TimeEpoch { get; set; }

        /// <summary>
        /// Calculates the time until this task executes.
        /// </summary>
        public TimeSpan GetTimeRemaining()
        {
            return TimeEpoch - DateTime.UtcNow;
        }
    }
}