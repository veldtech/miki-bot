using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.SDK.Interfaces
{
    public interface IDiscordUser : IDiscordEntity, IMentionable
    {
        string AvatarUrl { get; }

        bool IsBot { get; }

        string Discriminator { get; }

        IDiscordAudioChannel VoiceChannel { get; }

        int Hierarchy { get; }

        IDiscordGuild Guild { get; }

        DateTimeOffset CreatedAt { get; }
        DateTimeOffset? JoinedAt { get; }

        List<ulong> RoleIds { get; }

        string Username { get; }
        string Nickname { get; }

        bool HasPermissions(IDiscordChannel channel, params DiscordGuildPermission[] permissions);

        Task AddRoleAsync(IDiscordRole role);

        Task AddRolesAsync(List<IDiscordRole> roles);

        Task Ban(IDiscordGuild guild, int pruneDays = 0, string reason = "");

        Task Kick(string reason = "");

        string GetAvatarUrl(DiscordAvatarType type = DiscordAvatarType.PNG, ushort size = 128);
        string GetName();

        Task RemoveRoleAsync(IDiscordRole role);

        Task RemoveRolesAsync(List<IDiscordRole> roles);

        Task SendFile(string path);

        Task<IDiscordMessage> SendMessage(string text);

        Task<IDiscordMessage> SendMessage(IDiscordEmbed embed);

        Task SetNickname(string text);

        Task Unban(IDiscordGuild guild);
    }

    public enum DiscordAvatarType
    {
        PNG, GIF
    };
}