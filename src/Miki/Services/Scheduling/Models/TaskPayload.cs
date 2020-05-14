using System;
using System.Runtime.Serialization;

namespace Miki.Services.Scheduling
{
    [DataContract]
    public class TaskPayload 
    {
        /// <summary>
        /// Universally unique ID
        /// </summary>
        [DataMember(Name = "id", Order = 1)]
        public string Uuid { get; set; }

        /// <summary>
        /// Task attached to this worker.
        /// </summary>
        [DataMember(Name = "task_name", Order = 2)]
        public string TaskName { get; set; }

        /// <summary>
        /// Unique identifier to seperate this from it's siblings.
        /// </summary>
        [DataMember(Name = "task_id", Order = 3)]
        public string TaskId { get; set; }

        /// <summary>
        /// Unique identifier to seperate this from it's siblings.
        /// </summary>
        [DataMember(Name = "owner_id", Order = 4)]
        public string OwnerId { get; set; }

        /// <summary>
        /// Time when this task has started. This time is required to be UTC.
        /// </summary>
        [DataMember(Name = "start_time", Order = 5)]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Time for when to call.
        /// </summary>
        [DataMember(Name = "duration", Order = 6)]
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Json string of the payload sent as the response.,,
        /// </summary>
        [DataMember(Name = "payload", Order = 7)]
        public string PayloadJson { get; set; }

        /// <summary>
        /// Whether the task repeats itself after execution.
        /// </summary>
        [DataMember(Name = "is_repeating", Order = 8)]
        public bool IsRepeating { get; set; }

        /// <summary>
        /// The time in epoch when the 
        /// </summary>
        [DataMember(Name = "time_epoch", Order = 9)]
        public Epoch TimeEpoch { get; set; }

        /// <summary>
        /// Gets the cache key for the task queue
        /// </summary>
        public TaskKey GetKey()
        {
            return new TaskKey
            {
                Uuid = Uuid,
                OwnerId = OwnerId
            };
        }

        /// <summary>
        /// Calculates the time until this task executes.
        /// </summary>
        public TimeSpan GetTimeRemaining()
        {
            return TimeEpoch - DateTime.UtcNow;
        }
    }
}