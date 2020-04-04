namespace Miki.Services.Lottery
{
    using System.Runtime.Serialization;

    [DataContract]
    public class LotteryEntry
    {
        /// <summary>
        /// The User ID of which this user belongs to.
        /// </summary>
        [DataMember(Name = "user_id", Order = 1)]
        public long UserId { get; set; }

        /// <summary>
        /// Amount of tickets bought by the user
        /// </summary>
        [DataMember(Name = "ticket_count", Order = 2)]
        public int TicketCount { get; set; }
    }
}