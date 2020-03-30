namespace Miki.Tests.Services
{
    using Miki.Bot.Models;

    public class AchievementServiceTests : BaseEntityTest<MikiDbContext>
    {
        public AchievementServiceTests()
            : base(x => new MikiDbContext(x))
        {
            
        }
    }   
}
