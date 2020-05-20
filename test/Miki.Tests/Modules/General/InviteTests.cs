using System.Threading.Tasks;
using Miki.Discord.Common;
using Miki.Discord.Internal;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Stages;
using Miki.Modules;
using Moq;
using Xunit;

namespace Miki.Tests.Modules.General
{
    public class InviteTests : BaseCommandTest
    {
        [Fact]
        public async Task InviteAsync()
        {
            var userMock = new Mock<IDiscordUser>();
            userMock.Setup(x => x.GetDMChannelAsync())
                .Returns(Task.FromResult<IDiscordTextChannel>(
                    new DiscordGuildTextChannel(new DiscordChannelPacket(), null)));

            var messageMock = new Mock<IDiscordMessage>();
            messageMock.SetupGet(x => x.Author)
                .Returns(userMock.Object);
            
            Mock.SetContext(
                FetchDataStage.ChannelArgumentKey, 
                new DiscordGuildTextChannel(new DiscordChannelPacket(), null));
            Mock.SetContext(CorePipelineStage.MessageContextKey, messageMock.Object);

            var general = new GeneralModule();
            await general.InviteAsync(Mock);

            Assert.True(Worker.TryGetMessage(out var response));
            Assert.Equal("miki_module_general_invite_message", response.Arguments.Properties.Content);

            Assert.True(Worker.TryGetMessage(out response));
            Assert.StartsWith("miki_module_general_invite_dm", response.Arguments.Properties.Content);
        }
    }
}
