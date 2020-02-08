namespace Miki.Tests.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Discord.Common;
    using Discord.Common.Packets;
    using Discord.Common.Packets.API;
    using Discord.Internal;
    using Framework;
    using Framework.Commands.Localization;
    using Localization.Models;
    using Moq;
    
    public class MockResourceManager : IResourceManager
    {
        private readonly Func<string, string> keyFactory;

        public MockResourceManager(Func<string, string> keyFactory)
        {
            this.keyFactory = keyFactory;
        }

        public static MockResourceManager PassThrough => new MockResourceManager(x => x);

        /// <inheritdoc />
        public string GetString(string key)
        {
            return keyFactory(key);
        }
    }

    public class BaseCommandTest
    {
        public MockMessageWorker Worker { get; } = new MockMessageWorker();
        public Mock<IContext> Mock { get; } = new Mock<IContext>();

        public BaseCommandTest()
        {
            InitWorker(Mock);
            InitLocale("eng");
        }

        public void InitContext<T>(string key, T value)
        {
            Mock.Setup(x => x.GetContext<T>(key))
                .Returns(value);
        }

        public void InitService<T>(Mock<IContext> mock, T returnType)
        {
            mock.Setup(x => x.GetService(It.Is<Type>(type => type == typeof(T))))
                .Returns(returnType);
        }

        public void InitLocale(string locale, IResourceManager resource = null)
        {
            Mock.Setup(x => x.GetContext<Locale>(LocalizationPipelineStage.LocaleContext))
                .Returns(new Locale(locale, resource ?? MockResourceManager.PassThrough));
        }

        public void InitWorker(Mock<IContext> mock) 
            => InitService<IMessageWorker<IDiscordMessage>>(mock, Worker);
    }

    public class MockMessageWorker : IMessageWorker<IDiscordMessage>
    {
        private class MockDiscordMessage : IDiscordMessage
        {
            private readonly MockMessageWorker worker;
            private readonly IMessageReference<IDiscordMessage> reference;

            public MockDiscordMessage(MockMessageWorker worker, IMessageReference<IDiscordMessage> reference)
            {
                this.worker = worker;
                this.reference = reference;
            }
            
            public ulong Id => 0;
            public IReadOnlyList<IDiscordAttachment> Attachments => null;
            public IDiscordUser Author => null;
            public string Content => reference.Arguments.Properties.Content;
            public ulong ChannelId => 0;
            public IReadOnlyList<ulong> MentionedUserIds => new List<ulong>();
            public DateTimeOffset Timestamp => DateTimeOffset.Now;
            public DiscordMessageType Type => 0;
            public Task CreateReactionAsync(DiscordEmoji emoji)
            {
                return Task.CompletedTask;
            }
            public Task DeleteReactionAsync(DiscordEmoji emoji)
            {
                return Task.CompletedTask;
            }
            public Task DeleteReactionAsync(DiscordEmoji emoji, IDiscordUser user)
            {
                return Task.CompletedTask;
            }
            public Task DeleteReactionAsync(DiscordEmoji emoji, ulong userId)
            {
                return Task.CompletedTask;
            }
            public Task DeleteAllReactionsAsync()
            {
                return Task.CompletedTask;
            }
            public Task<IDiscordMessage> EditAsync(EditMessageArgs args)
            {
                worker.CreateRef(new MessageBucketArgs
                {
                    Properties = new MessageArgs
                    {
                        Content = args.Content,
                        Embed = args.Embed
                    }
                });
                return Task.FromResult<IDiscordMessage>(this);
            }
            public Task DeleteAsync()
            {
                return Task.CompletedTask;
            }
            public Task<IDiscordTextChannel> GetChannelAsync()
            {
                return Task.FromResult<IDiscordTextChannel>(null);
            }
            public Task<IEnumerable<IDiscordUser>> GetReactionsAsync(DiscordEmoji emoji)
            {
                return Task.FromResult<IEnumerable<IDiscordUser>>(new List<IDiscordUser>());
            }
        }

        private readonly Queue<MessageReference> caughtMessages = new Queue<MessageReference>();

        public bool TryGetMessage(out MessageReference args)
        {
            if (caughtMessages.Any())
            {
                args = caughtMessages.Dequeue();
                return true;
            }
            args = null;
            return false;
        }

        /// <inheritdoc />
        public IMessageReference<IDiscordMessage> CreateRef(MessageBucketArgs args)
        {
            var m = new MessageReference(args);
            caughtMessages.Enqueue(m);
            return m;
        }

        public void Execute(IMessageReference<IDiscordMessage> @ref)
        {
            Run(@ref);
        }

        public void Run(IMessageReference<IDiscordMessage> r)
        {
            foreach (var t in r.Decorators)
            {
                t(new MockDiscordMessage(this, r));
            }
        }
    }
}
