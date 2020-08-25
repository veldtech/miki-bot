using System.Runtime.Serialization;

namespace Miki.API.Payments.Data
{
    [DataContract]
    public class GiftCodeRedeemResponse
    {
        [DataMember(Name = "gift")]
        public GiftCode GiftCode { get; set; }
        
        [DataMember(Name = "subscriber")]
        public Subscriber Subscriber { get; set; }
    }
}