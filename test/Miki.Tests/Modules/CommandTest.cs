using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands.Localization;
using Miki.Localization;

namespace Miki.Tests.Modules
{
    public class BaseCommandTest
    {
        public MockMessageWorker Worker { get; } = new MockMessageWorker();
        public TestContextObject Mock { get; } = new TestContextObject();

        public BaseCommandTest()
        {
            InitWorker();
            InitLocale("eng");
        }

        public void InitLocale(string locale, IResourceManager resource = null)
        {
            Mock.SetContext(
                LocalizationPipelineStage.LocaleContextKey,
                new Locale(locale, resource ?? TestResourceManager.PassThrough));
        }

        public void InitWorker()
            => Mock.SetService(typeof(IMessageWorker<IDiscordMessage>), Worker);
    }
}
