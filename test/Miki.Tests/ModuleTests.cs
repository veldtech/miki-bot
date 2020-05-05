namespace Miki.Tests
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Bot.Models;
    using Miki.Framework;
    using Moq;
    using Xunit;

    public class ModuleTests
    {
        [Fact(DisplayName = "App should run with no dependency injection issues on self host.")]
        public async Task DependencyInjectionTestAsync()
        {
            var mock = new Mock<IStartupConfiguration>();
            mock.Setup(x => x.IsSelfHosted).Returns(true);
            mock.Setup(x => x.ConnectionString).Returns("some string");
            mock.Setup(x => x.Configuration).Returns(
                new Config
                {
                    Token = "not a real value",
                });

            var app = new MikiBotApp(mock.Object);
            var collection = new ServiceCollection();
            collection.AddSingleton<MikiApp, MikiBotApp>();
            await app.ConfigureAsync(collection);
        }

        [Fact(DisplayName = "App should run with no dependency injection issues on production.")]
        public async Task DependencyInjectionProductionTestAsync()
        {
            var mock = new Mock<IStartupConfiguration>();
            mock.Setup(x => x.ConnectionString).Returns("some string");
            mock.Setup(x => x.Configuration).Returns(
                new Config
                {
                    Token = "not a real value",
                });

            var app = new MikiBotApp(mock.Object);
            var collection = new ServiceCollection();
            collection.AddSingleton<MikiApp, MikiBotApp>();
            await app.ConfigureAsync(collection);
        }
    }
}
