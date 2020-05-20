using System;
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
    public class PingTests : BaseCommandTest
    {
        [Fact]
        public async Task PingAsync()
        {
            var messageMock = new Mock<IDiscordMessage>();
            messageMock.SetupGet(x => x.Timestamp)
                .Returns(DateTimeOffset.Now);

            Mock.SetContext(
                FetchDataStage.ChannelArgumentKey, 
                new DiscordGuildTextChannel(new DiscordChannelPacket(), null));
            Mock.SetContext(CorePipelineStage.MessageContextKey, messageMock.Object);

            var general = new GeneralModule();
            await general.PingAsync(Mock);

            Assert.True(Worker.TryGetMessage(out var response));

            Assert.NotNull(response.Arguments.Properties.Embed);
            Assert.Equal("Ping", response.Arguments.Properties.Embed.Title);
            Assert.Equal("ping_placeholder", response.Arguments.Properties.Embed.Description);

            Assert.True(Worker.TryGetMessage(out response));
            
            Assert.NotNull(response.Arguments.Properties.Embed);
            Assert.Equal($"Pong - {Environment.MachineName}", 
                response.Arguments.Properties.Embed.Title);
        }
    }
}
