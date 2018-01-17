using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.SDK.Interfaces
{
    public interface IDiscordMessage : IDiscordEntity
    {
        IReadOnlyCollection<IDiscordAttachment> Attachments { get; }

        IDiscordUser Author { get; }
        IDiscordUser Bot { get; }

        IDiscordClient Discord { get; }

        IDiscordMessageChannel Channel { get; }
        IDiscordAudioChannel VoiceChannel { get; }

        IDiscordGuild Guild { get; }

        Dictionary<DiscordEmoji, DiscordReactionMetadata> Reactions { get; }

        string Content { get; }
		string ResolvedContent { get; }

        DateTimeOffset Timestamp { get; }

        IReadOnlyCollection<ulong> MentionedUserIds { get; }
        IReadOnlyCollection<ulong> MentionedRoleIds { get; }
        IReadOnlyCollection<ulong> MentionedChannelIds { get; }

        Task DeleteAsync();

        Task ModifyAsync(string message);

        Task ModifyAsync(IDiscordEmbed embed);

        Task PinAsync();

        Task UnpinAsync();
    }
}