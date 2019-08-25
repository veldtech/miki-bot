using System;
using System.Collections.Generic;
using Miki.Bot.Models;

namespace Miki.Services.Achievements
{
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

            public Builder AddEntry(string name, string icon)
            {
                if (entries == null)
                {
                    entries = new List<AchievementEntry>();
                }
                entries.Add(new AchievementEntry(null, name, icon, (short)entries.Count));
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

        public string ResourceName { get; }
        public string Icon { get; }
        public short  Rank { get; }
        public int Points => (1 + Rank) * 5;

        private readonly AchievementObject parent;

        public AchievementEntry(AchievementObject parent, string name, string icon, short rank)
        {
            this.parent = parent;
            ResourceName = name;
            Icon = icon;
            Rank = rank;
        }

        public Achievement ToModel(long userId)
        {
            return new Achievement
            {
                Name = parent.Id,
                UnlockedAt = DateTime.UtcNow,
                Rank = Rank,
                UserId = userId
            };
        }
    }
}