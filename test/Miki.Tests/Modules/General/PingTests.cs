namespace Miki.Tests.Modules.General
{
    using System;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Common;
    using Discord.Common.Packets;
    using Discord.Internal;
    using Framework;
    using Framework.Commands;
    using Framework.Commands.Localization;
    using Framework.Commands.Stages;
    using Localization.Models;
    using Miki.Modules;
    using Moq;
    using Xunit;

    public class PingTests : BaseCommandTest
    {
        [Fact]
        public async Task Ping()
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
