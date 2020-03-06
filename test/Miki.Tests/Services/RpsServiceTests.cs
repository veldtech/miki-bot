namespace Miki.Tests.Services
{
    using Miki.Services.Rps;
    using Xunit;

    public class RpsServiceTests
    {
        private readonly RpsService service;

        public RpsServiceTests()
        {
            this.service = new RpsService(
                null);
        }

        [Theory]
        [InlineData(0, 1, GameResult.Win)]
        [InlineData(1, 2, GameResult.Win)]
        [InlineData(2, 3, GameResult.Win)]
        [InlineData(0, 0, GameResult.Draw)]
        [InlineData(1, 1, GameResult.Draw)]
        [InlineData(2, 2, GameResult.Draw)]
        [InlineData(1, 0, GameResult.Lose)]
        [InlineData(2, 1, GameResult.Lose)]
        [InlineData(0, 2, GameResult.Lose)]
        public void GameResultTest(int a, int b, GameResult result)
        {
            Assert.Equal(service.CalculateVictory(a, b), result);
        }

        [Theory]
        [InlineData("w", null)]
        [InlineData("r", "Rock")]
        [InlineData("p", "Paper")]
        [InlineData("s", "Scissors")]
        [InlineData(null, null)]
        public void GetWeaponTests(string input, string expectedWeapon)
        {
            Assert.Equal(expectedWeapon, service.GetWeapon(input).UnwrapDefault()?.Name);
        }
    }
}
