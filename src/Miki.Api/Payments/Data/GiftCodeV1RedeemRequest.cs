using System;
using System.Runtime.Serialization;

namespace Miki.API.Payments.Data
{
    [DataContract]
    public class GiftCodeV1RedeemRequest
    {
        [DataMember(Name = "key")]
        public Guid Key { get; set; }
        
        [DataMember(Name = "user_id")]
        public long UserId { get; set; }
    }
}