using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IA.SDK
{
    public class DiscordUser : IDiscordUser, IMentionable
    {
        public virtual string AvatarUrl
        {
            get
            {
                return "";
            }
        }

        public DateTimeOffset CreatedAt
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual string Discriminator
        {
            get
            {
                return "";
            }
        }

        public virtual IDiscordGuild Guild
        {
            get
            {
                return null;
            }
        }

        public virtual ulong Id
        {
            get
            {
                return 0;
            }
        }

        public virtual bool IsBot
        {
            get
            {
                return false;
            }
        }

        public DateTimeOffset? JoinedAt
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual string Mention
        {
            get
            {
                return "<@!" + Id + ">";
            }
        }

        public string Nickname
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual List<ulong> RoleIds
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual string Username
        {
            get
            {
                return "";
            }
        }

        public IDiscordAudioChannel VoiceChannel
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Hierarchy => throw new NotImplementedException();

        public virtual Task AddRoleAsync(IDiscordRole role)
        {
            throw new NotImplementedException();
        }

        public Task AddRolesAsync(List<IDiscordRole> roles)
        {
            throw new NotImplementedException();
        }

        public virtual Task Ban(IDiscordGuild guild, int amount = 0, string reason = "")
        {
            throw new NotImplementedException();
        }

        public string GetAvatarUrl(DiscordAvatarType type = DiscordAvatarType.PNG, ushort size = 128)
        {
            throw new NotImplementedException();
        }

        public string GetName()
        {
            throw new NotImplementedException();
        }

        public virtual bool HasPermissions(IDiscordChannel channel, params DiscordGuildPermission[] permissions)
        {
            throw new NotImplementedException();
        }

        public virtual Task Kick(string x = "")
        {
            throw new NotImplementedException();
        }

        public virtual Task RemoveRoleAsync(IDiscordRole role)
        {
            throw new NotImplementedException();
        }

        public Task RemoveRolesAsync(List<IDiscordRole> roles)
        {
            throw new NotImplementedException();
        }

        public virtual Task SendFile(string path)
        {
            throw new NotImplementedException();
        }

        public virtual Task<IDiscordMessage> SendMessage(string text)
        {
            throw new NotImplementedException();
        }

        public virtual Task<IDiscordMessage> SendMessage(IDiscordEmbed embed)
        {
            throw new NotImplementedException();
        }

        public Task SetNickname(string text)
        {
            throw new NotImplementedException();
        }

        public Task Unban(IDiscordGuild guild)
        {
            throw new NotImplementedException();
        }
    }
}