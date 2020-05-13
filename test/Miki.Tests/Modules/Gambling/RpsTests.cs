using System.Threading.Tasks;
using Miki.Bot.Models;
using Miki.Discord.Common;
using Miki.Framework.Arguments;
using Miki.Framework.Commands;
using Miki.Modules.Gambling;
using Miki.Services;
using Miki.Services.Rps;
using Moq;
using Xunit;

namespace Miki.Tests.Modules.Gambling
{
    public class RpsTests : BaseCommandTest
    {
        [Fact]
        public async Task RpsWinTest()
        {
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserAsync(It.IsAny<long>()))
                .Returns(new ValueTask<User>(new User
                {
                    Currency = 20
                }));
            Mock.SetService(typeof(IUserService), userServiceMock.Object);

            var userMock = new Mock<IDiscordUser>();

            var messageMock = new Mock<IDiscordMessage>();
            messageMock.Setup(x => x.Author)
                .Returns(userMock.Object);

            Mock.SetContext(CorePipelineStage.MessageContextKey, messageMock.Object);

            Mock.SetContext(
                "framework-arguments",
                new TypedArgumentPack(new ArgumentPack("10", "r"), new ArgumentParseProvider()));

            var serviceMock = new Mock<IRpsService>();
            serviceMock.Setup(x => x.PlayRpsAsync(
                    It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(
                    new RpsGameResult.Builder()
                        .WithAmountWon(10)
                        .WithBet(10)
                        .WithCpuWeapon(new RpsWeapon("scissor", "x"))
                        .WithPlayerWeapon(new RpsWeapon("rock", "x"))
                        .WithStatus(GameResult.Win)
                        .Build()));

            Mock.SetService(typeof(IRpsService), serviceMock.Object);

            var module = new GamblingModule.RpsCommand();
            await module.RpsAsync(Mock);

            Worker.TryGetMessage(out var msg);

            Assert.Equal(
                "ROCK x vs. x SCISSOR\n\ngame_result_win currency_update_balance",
                msg.Arguments.Properties.Embed.Description);
        }
    }
}
