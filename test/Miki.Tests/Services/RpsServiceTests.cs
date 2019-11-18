namespace Miki.Tests.Services
{
    using Miki.Services.Rps;
    using Xunit;

    public class RpsServiceTests
    {
        private readonly RpsService service;

        public RpsServiceTests()
        {
            this.service = new RpsService();
        }

        [Theory]
        [InlineData(0, 1, RpsService.VictoryStatus.WIN)]
        [InlineData(1, 2, RpsService.VictoryStatus.WIN)]
        [InlineData(2, 3, RpsService.VictoryStatus.WIN)]
        [InlineData(0, 0, RpsService.VictoryStatus.DRAW)]
        [InlineData(1, 1, RpsService.VictoryStatus.DRAW)]
        [InlineData(2, 2, RpsService.VictoryStatus.DRAW)]
        [InlineData(1, 0, RpsService.VictoryStatus.LOSE)]
        [InlineData(2, 1, RpsService.VictoryStatus.LOSE)]
        [InlineData(0, 2, RpsService.VictoryStatus.LOSE)]
        public void GameResultTest(int a, int b, RpsService.VictoryStatus result)
        {
            Assert.Equal(service.CalculateVictory(a, b), result);
        }
    }
}
