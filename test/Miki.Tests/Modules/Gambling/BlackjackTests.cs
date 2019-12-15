namespace Miki.Tests.Modules.Gambling
{
    using System;
    using System.Threading.Tasks;
    using Cache;
    using Discord.Common;
    using Discord.Internal;
    using Framework.Commands;
    using Framework.Commands.Stages;
    using Miki.Modules;
    using Miki.Modules.Gambling;
    using Moq;
    using Xunit;

    public class BlackjackTests : BaseCommandTest
    {
        [Fact]
        public async Task DefaultBlackjack()
        {
            Mock.Setup(x => x.GetContext<IDiscordTextChannel>(FetchDataStage.ChannelArgumentKey))
                .Returns(new DiscordGuildTextChannel(new DiscordChannelPacket(), null));

           // var command = new GamblingModule.BlackjackCommand();
           // await command.BlackjackAsync(Mock.Object);

            Assert.True(Worker.TryGetMessage(out var response));
            Assert.NotNull(response.Arguments.Properties.Embed);
        }

        [Fact]
        public async Task NewBlackjack()
        {
            var cache = new Mock<ICacheClient>();

            InitService(Mock, cache.Object);

            Mock.Setup(x => x.GetContext<IDiscordTextChannel>(FetchDataStage.ChannelArgumentKey))
                .Returns(new DiscordGuildTextChannel(new DiscordChannelPacket(), null));

            //var command = new GamblingModule.BlackjackCommand();
            //await command.BlackjackNewAsync(Mock.Object);

            Assert.True(Worker.TryGetMessage(out var response));
            Assert.NotNull(response.Arguments.Properties.Embed);
        }
    }
}
