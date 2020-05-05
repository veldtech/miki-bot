namespace Miki.Services.Achievements
{
    using System;
    using System.Collections.Generic;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Miki.Bot.Models;

    public class AchievementObject
    {
        public string Id { get; private set; }
        public IReadOnlyList<AchievementEntry> Entries { get; private set; }

        public class Builder
        {
            private readonly string id;
            private List<AchievementEntry> entries;

            public Builder(string id)
            {
                this.id = id;
            }

            public Builder Add(string icon)
            {
                if(entries == null)
                {
                    entries = new List<AchievementEntry>();
                }

                entries.Add(
                    new AchievementEntry(
                        id, icon, (short)entries.Count));
                return this;
            }

            public AchievementObject Build()
            {
                return new AchievementObject
                {
                    Id = id,
                    Entries = entries
                };
            }
        }
    }

    public class AchievementEntry
    {
        public string ResourceName => $"achievement_{Id}_{Rank}";
        public string Id { get; set; }
        public string Icon { get; }
        public short Rank { get; }
        public int Points => (1 + Rank) * 5;

        public AchievementEntry(string id, string icon, short rank)
        {
            Id= id;
            Icon = icon;
            Rank = rank;
        }

        public Achievement ToModel(ulong userId) 
            => ToModel((long)userId);
        public Achievement ToModel(long userId)
        {
            return new Achievement
            {
                Name = Id,
                UnlockedAt = DateTime.UtcNow,
                Rank = Rank,
                UserId = userId
            };
        }
    }
}