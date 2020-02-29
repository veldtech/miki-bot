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
        [InlineData(0, 1, VictoryStatus.WIN)]
        [InlineData(1, 2, VictoryStatus.WIN)]
        [InlineData(2, 3, VictoryStatus.WIN)]
        [InlineData(0, 0, VictoryStatus.DRAW)]
        [InlineData(1, 1, VictoryStatus.DRAW)]
        [InlineData(2, 2, VictoryStatus.DRAW)]
        [InlineData(1, 0, VictoryStatus.LOSE)]
        [InlineData(2, 1, VictoryStatus.LOSE)]
        [InlineData(0, 2, VictoryStatus.LOSE)]
        public void GameResultTest(int a, int b, VictoryStatus result)
        {
            Assert.Equal(service.CalculateVictory(a, b), result);
        }
    }
}
