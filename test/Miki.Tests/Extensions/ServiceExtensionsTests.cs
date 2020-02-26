namespace Miki.Tests.Extensions
{
    using System.Threading.Tasks;
    using Miki.Bot.Models.Exceptions;
    using Miki.Discord.Common;
    using Miki.Services;
    using Moq;
    using Xunit;

    public class ServiceExtensionsTests
    {
        [Fact]
        public async Task VerifyGetOrCreateUserAsync()
        {
            var userMock = new Mock<IDiscordUser>();
            userMock.Setup(x => x.Id).Returns(0L);
            userMock.Setup(x => x.Username).Returns("test");

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(x => x.GetUserAsync(It.IsAny<long>()))
                .Throws(new UserNullException());

            await userServiceMock.Object.GetOrCreateUserAsync(userMock.Object);

            userServiceMock.Verify(
                x => x.CreateUserAsync(It.Is<long>(x => x == 0), It.Is<string>(x => x == "test")));
        } 
    }
}
