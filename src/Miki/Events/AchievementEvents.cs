using System;
using System.Reactive.Subjects;
using Miki.Framework;
using Miki.Services.Achievements;

namespace Miki
{
    public class AchievementEvents
    {
        private readonly Subject<(IContext, AchievementEntry)> achievementUnlockedByUserSubject;
        private readonly Subject<AchievementEntry> achievementUnlockedSubject;

        public AchievementEvents()
        {
            achievementUnlockedByUserSubject = new Subject<(IContext, AchievementEntry)>();
            achievementUnlockedSubject = new Subject<AchievementEntry>();
        }

        public IObservable<(IContext, AchievementEntry)> OnAchievementUnlockedByUser
    => achievementUnlockedByUserSubject;
        public IObservable<AchievementEntry> OnAchievementUnlocked
            => achievementUnlockedSubject;

        public void CallAchievementUnlockedByUser(IContext context, AchievementEntry entry)
        {
            achievementUnlockedByUserSubject.OnNext((context, entry));
        }

        public void CallAchievementUnlocked(AchievementEntry entry)
        {
            achievementUnlockedSubject.OnNext(entry);
        }
    }
}