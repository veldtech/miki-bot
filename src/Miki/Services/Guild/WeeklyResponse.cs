namespace Miki.Services
{
    using System;
    using Miki.Bot.Models;

    public enum WeeklyStatus
    {
        Success,
        UserInsufficientExp,
        GuildInsufficientExp,
        RivalNull,
        NotReady
    }

    public class WeeklyResponse
    {
        public WeeklyStatus Status { get; }
        public int AmountClaimed { get; }
        public DateTime LastClaimTime { get; }
        public int ExperienceNeeded { get; }

        public WeeklyResponse(WeeklyStatus status, int amountClaimed, DateTime lastClaimTime, int experienceNeeded = 0)
        {
            Status = status;
            AmountClaimed = amountClaimed;
            LastClaimTime = lastClaimTime;
            ExperienceNeeded = experienceNeeded;
        }
    }
}
