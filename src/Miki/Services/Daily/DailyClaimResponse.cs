namespace Miki.Services.Daily
{
    using System;
    using Miki.Bot.Models;

    public enum DailyStatus
    {
        Success,
        Claimed
    }

    public class DailyClaimResponse
    {

        public DailyStatus Status { get; set; }
        public int AmountClaimed { get; set; }
        public int LongestStreak { get; set; }
        public int CurrentStreak { get; set; }
        public DateTime LastClaimTime { get; set; }

        public DailyClaimResponse(Daily daily, DailyStatus status, int amountClaimed)
        {
            Status = status;
            AmountClaimed = amountClaimed;
            LongestStreak = daily.LongestStreak;
            CurrentStreak = daily.CurrentStreak;
            LastClaimTime = daily.LastClaimTime;
        }
    }
}
