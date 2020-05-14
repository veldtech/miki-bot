using System.Threading.Tasks;
using Miki.Discord.Common;
using Miki.Discord.Internal;
using Miki.Framework.Commands.Stages;
using Miki.Modules.Gambling;
using Xunit;

namespace Miki.Tests.Modules.Gambling
{
    public class BlackjackTests : BaseCommandTest
    {
        [Fact]
        public async Task DefaultBlackjackAsync()
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
