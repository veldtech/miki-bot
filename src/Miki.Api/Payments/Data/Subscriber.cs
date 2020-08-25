using System;
using System.Runtime.Serialization;

namespace Miki.API.Payments.Data
{
    [DataContract]
    public class Subscriber
    {
        [DataMember(Name = "user_id")]
        public long UserId { get; set; }
            
        [DataMember(Name = "status")]
        public SubscriptionStatus Status { get; set; }

        [DataMember(Name = "valid_until")]
        public DateTime? ValidUntil { get; set; }

        [DataMember(Name = "created_at")]
        public DateTime? CreatedAt { get; set; }
    }
    
    public enum SubscriptionStatus
    {
        Active,
        ActiveTemporary,
        Inactive,
        Banned,
    }
}