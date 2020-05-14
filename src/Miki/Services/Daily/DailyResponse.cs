using System;
using Miki.Bot.Models;

namespace Miki.Services.Daily
{
    public enum DailyStatus
    {
        Success,
        NotReady
    }

    public class DailyResponse
    {
        public DailyStatus Status { get; set; }
        public int AmountClaimed { get; set; }
        public int LongestStreak { get; set; }
        public int CurrentStreak { get; set; }
        public DateTime LastClaimTime { get; set; }

        public DailyResponse(Daily daily, DailyStatus status, int amountClaimed)
        {
            Status = status;
            AmountClaimed = amountClaimed;
            LongestStreak = daily.LongestStreak;
            CurrentStreak = daily.CurrentStreak;
            LastClaimTime = daily.LastClaimTime;
        }
    }
}
