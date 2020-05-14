using System.Runtime.Serialization;

namespace Miki.Services.Scheduling
{
    [DataContract]
    public class TaskKey
    {
        /// <summary>
        /// Universally unique ID
        /// </summary>
        [DataMember(Name = "id", Order = 1)]
        public string Uuid { get; set; }

        /// <summary>
        /// Unique identifier to seperate this from it's siblings.
        /// </summary>
        [DataMember(Name = "owner_id", Order = 2)]
        public string OwnerId { get; set; }
    }
}