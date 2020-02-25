namespace Miki.Tests.Modules
{
    using System;
    using Discord.Common;
    using Framework;
    using Framework.Commands.Localization;
    using Localization.Models;
    using Moq;

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
                new Locale(locale, resource ?? MockResourceManager.PassThrough));
        }

        public void InitWorker()
            => Mock.SetService(typeof(IMessageWorker<IDiscordMessage>), Worker);
    }
}
