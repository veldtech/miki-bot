namespace Miki.Tests.Modules.Gambling
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cache;
    using Discord.Common;
    using Discord.Internal;
    using Framework.Commands;
    using Framework.Commands.Stages;
    using Miki.Bot.Models;
    using Miki.Discord.Common.Packets;
    using Miki.Discord.Common.Packets.API;
    using Miki.Framework.Arguments;
    using Miki.Framework.Commands.Pipelines;
    using Miki.Modules;
    using Miki.Modules.Gambling;
    using Miki.Services;
    using Moq;
    using Xunit;

    public class BlackjackTests : BaseCommandTest
    {
        [Fact]
        public async Task DefaultBlackjack()
        {
            Mock.Setup(x => x.GetContext<IDiscordTextChannel>(FetchDataStage.ChannelArgumentKey))
                .Returns(new DiscordGuildTextChannel(new DiscordChannelPacket(), null));

            var command = new GamblingModule.BlackjackCommand();
            await command.BlackjackAsync(Mock.Object);

            Assert.True(Worker.TryGetMessage(out var response));
            Assert.NotNull(response.Arguments.Properties.Embed);
        }
    }
}
