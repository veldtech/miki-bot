using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Miki.API.Payments.Data
{
    [DataContract]
    public class GiftCode
    {
        [DataMember(Name = "hash")]
        public string Hash { get; set; }
        
        [DataMember(Name = "type")]
        public int Type { get; set; }
        
        [DataMember(Name = "amount")]
        public int AmountDays { get; set; }
    }
}