namespace Miki.Services
{
    using System;
    using Miki.Bot.Models;

    public enum WeeklyStatus
    {
        Success,
        UserInsufficientExp,
        GuildInsufficientExp,
        NoRival,
        NotReady
    }

    public class WeeklyResponse
    {
        public WeeklyStatus Status { get; set; }
        public int AmountClaimed { get; set; }
        public DateTime LastClaimTime { get; set; }
        public int ExperienceNeeded { get; set; }

        public WeeklyResponse(WeeklyStatus status, int amountClaimed, DateTime lastClaimTime, int experienceNeeded = 0)
        {
            Status = status;
            AmountClaimed = amountClaimed;
            LastClaimTime = lastClaimTime;
            ExperienceNeeded = experienceNeeded;
        }
    }
}
