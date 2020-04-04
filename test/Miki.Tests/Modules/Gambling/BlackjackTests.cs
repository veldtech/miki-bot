namespace Miki.Tests.Modules.Gambling
{
    using System.Threading.Tasks;
    using Discord.Common;
    using Discord.Internal;
    using Framework.Commands.Stages;
    using Miki.Modules.Gambling;
    using Xunit;

    public class BlackjackTests : BaseCommandTest
    {
        [Fact]
        public async Task DefaultBlackjack()
        {
            Mock.SetContext(
                FetchDataStage.ChannelArgumentKey, 
                new DiscordGuildTextChannel(new DiscordChannelPacket(), null));

            var command = new GamblingModule.BlackjackCommand();
            await command.BlackjackAsync(Mock);

            Assert.True(Worker.TryGetMessage(out var response));
            Assert.NotNull(response.Arguments.Properties.Embed);
        }
    }
}
