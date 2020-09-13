using System;
using System.Reactive.Subjects;
using Miki.Framework;
using Miki.Services.Achievements;

namespace Miki
{
    public class AchievementEvents
    {
        private readonly Subject<(AchievementEntry, ulong)> achievementUnlockedSubject;

        public AchievementEvents()
        {
            achievementUnlockedSubject = new Subject<(AchievementEntry, ulong)>();
        }

        public IObservable<(AchievementEntry, ulong)> OnAchievementUnlocked
            => achievementUnlockedSubject;

        public void CallAchievementUnlocked(AchievementEntry entry, ulong userId)
        {
            achievementUnlockedSubject.OnNext((entry, userId));
        }
    }
}