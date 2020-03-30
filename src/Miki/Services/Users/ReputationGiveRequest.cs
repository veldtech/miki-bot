namespace Miki.Services
{
    public class GiveReputationRequest
    {
        public long RecieverId { get; set; }
        public int Amount { get; set; }
    }
}