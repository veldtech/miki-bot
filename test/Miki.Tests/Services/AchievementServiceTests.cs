namespace Miki.Tests.Services
{
    using System.Diagnostics.CodeAnalysis;
    using Bot.Models;
    using Framework.Tests;
    using Microsoft.EntityFrameworkCore;
    using Miki.Services.Achievements;

    public class AchievementServiceTests : BaseEntityTest<Achievement>
    {
        public AchievementServiceTests()
        {
            
        }

        protected override void OnModelCreating([NotNull] ModelBuilder builder)
        {
            builder.Entity<Achievement>()
                .HasKey(x => new {x.UserId, x.Name});
        }
    }
}
