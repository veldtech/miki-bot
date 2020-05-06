using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Miki.Discord.Common;
using Miki.Discord.Common.Packets.API;
using Miki.Framework;

namespace Miki.Tests
{
    public class MockMessageWorker : IMessageWorker<IDiscordMessage>
    {
        private class MockDiscordMessage : IDiscordMessage
        {
            private readonly MockMessageWorker worker;
            private readonly IMessageReference<IDiscordMessage> reference;

            public MockDiscordMessage(
                MockMessageWorker worker, IMessageReference<IDiscordMessage> reference)
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