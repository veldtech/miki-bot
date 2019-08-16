using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Miki.Accounts;
using Miki.Accounts.Achievements.Objects;
using Miki.Bot.Models;
using Miki.Discord;
using Miki.Discord.Common.Packets;
using Miki.Discord.Internal;
using Miki.Services.Achievements;
using Miki.Services.Achievements.Models;
using Moq;
using NUnit.Framework;

namespace Miki.Tests.Services
{
    public class AchievementServiceTests
    {
        private AchievementService service;
        private IServiceCollection services;

        [SetUp]
        public void SetUp()
        {
             services = new ServiceCollection();

            var dbContextMock = new Mock<DbContext>();
            dbContextMock.SetReturnsDefault<Achievement>(new Achievement
            {
                Name = "test",
                Rank = 0,
                UnlockedAt = new DateTime(),
                UserId = 0
            });
            dbContext = dbContextMock.Object;
            services.AddSingleton();


            service = new AchievementService(
                new Mock<AccountService>(
                    new Mock<IDiscordClient>().Object).Object);
            service.AddAchievement("test", new MessageAchievement
            {
                Name = "test-achievement",
                Icon = "icon",
                ParentName = "test-achievement",
                Points = 10,
                CheckMessage = (m) => new ValueTask<bool>(m.message.Content == "test")
            });

        }

        [Test]
        public async Task UnlockAchievementAsync()
        {
            IAchievement achievement = null;
            service.OnAchievementUnlocked += (a) =>
            {
                achievement = a.achievement;
                return Task.CompletedTask;
            };

            await service.GetContainerById("test")
                .CheckAsync(new MessageEventPacket
                {
                    discordChannel = null,
                    discordUser = null,
                    message = new DiscordMessage(
                        new DiscordMessagePacket
                        {

                            Content = "test",
                        }, null)
                });

            Assert.NotNull(achievement);
            Assert.AreEqual("test", achievement.Name);
        }
    }
}
