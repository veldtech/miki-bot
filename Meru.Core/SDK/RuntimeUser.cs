using Discord;
using Discord.WebSocket;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IA.SDK
{
    public class RuntimeUser : IDiscordUser, IProxy<IUser>
    {
        private IUser user;

        public RuntimeUser()
        {
        }

        public RuntimeUser(IUser author)
        {
            user = author;
        }

        public string AvatarUrl
        {
            get
            {
                return user.GetAvatarUrl();
            }
        }

        public DateTimeOffset CreatedAt
        {
            get
            {
                return user.CreatedAt;
            }
        }
            
        public string Discriminator
        {
            get
            {
                return user.Discriminator;
            }
        }

        public IDiscordGuild Guild
        {
            get
            {
                IGuildUser u = user as IGuildUser;

                if (u == null)
                {
                    return null;
                }

                return new RuntimeGuild(u.Guild);
            }
        }

        public int Hierarchy
        {
            get => (user as SocketGuildUser).Hierarchy;
        }

        public ulong Id
        {
            get => user.Id;
        }

        public bool IsBot
        {
            get => user.IsBot;
        }

        public DateTimeOffset? JoinedAt
        {
            get
            {
                return (user as IGuildUser)?.JoinedAt;
            }
        }

        public string Mention
        {
            get
            {
                return user.Mention;
            }
        }

        public string Nickname
        {
            get
            {
                return (user as SocketGuildUser)?.Nickname;
            }
        }

        public List<ulong> RoleIds
        {
            get
            {
                return (user as IGuildUser).RoleIds.ToList();
            }
        }

        public string Username
        {
            get
            {
                return user.Username;
            }
        }

        public IDiscordAudioChannel VoiceChannel
        {
            get
            {
                return new RuntimeAudioChannel((user as IGuildUser).VoiceChannel);
            }
        }

        public async Task Ban(IDiscordGuild g, int pruneDay = 0, string reason = null)
        {
            IGuild x = (g as IProxy<IGuild>).ToNativeObject();
            await x.AddBanAsync(user, pruneDay, reason);
        }

        public async Task AddRoleAsync(IDiscordRole role)
        {
            await (user as IGuildUser).AddRolesAsync(new List<IRole> { (role as IProxy<IRole>).ToNativeObject() });
        }

        public async Task AddRolesAsync(List<IDiscordRole> roles)
        {
            List<IRole> roleList = new List<IRole>();

            foreach (IDiscordRole a in roles)
            {
                roleList.Add((a as IProxy<IRole>).ToNativeObject());
            }

            IGuildUser u = (user as IGuildUser);

            await u.AddRolesAsync(roleList);
        }

        public string GetAvatarUrl(DiscordAvatarType type = DiscordAvatarType.PNG, ushort size = 128)
        {
            ImageFormat i = ImageFormat.Png;
            if (type == DiscordAvatarType.GIF) i = ImageFormat.Gif;

            return user.GetAvatarUrl(i, size);
        }
        
        public string GetName()
        {
            if(string.IsNullOrWhiteSpace(Nickname))
            {
                return Username;
            }
            return Nickname;
        }

        public bool HasPermissions(IDiscordChannel channel, params DiscordGuildPermission[] permissions)
        {
            foreach (DiscordGuildPermission p in permissions)
            {
                GuildPermission newP = (GuildPermission)Enum.Parse(typeof(DiscordGuildPermission), p.ToString());

                IGuildChannel c = ((channel as IProxy<IChannel>).ToNativeObject() as IGuildChannel);

                if (!(user as IGuildUser).GuildPermissions.Has(newP))
                {
                    return false;
                }
            }
            return true;
        }

        public async Task Kick(string reason = null)
        {
            await (user as IGuildUser).KickAsync(reason);
        }

        public async Task RemoveRoleAsync(IDiscordRole role)
        {
            IRole r = (role as IProxy<IRole>).ToNativeObject();
            IGuildUser u = (user as IGuildUser);

            await u.RemoveRolesAsync(new List<IRole> { r });
        }

        public async Task RemoveRolesAsync(List<IDiscordRole> roles)
        {
            List<IRole> roleList = new List<IRole>();

            foreach (IDiscordRole a in roles)
            {
                roleList.Add((a as IProxy<IRole>).ToNativeObject());
            }

            IGuildUser u = (user as IGuildUser);

            await u.RemoveRolesAsync(roleList);
        }

        public async Task SendFile(string path)
        {
            IDMChannel c = await user.GetOrCreateDMChannelAsync();
            await c.SendFileAsync(path);
        }

        public async Task<IDiscordMessage> SendMessage(string message)
        {
            IDMChannel c = await user.GetOrCreateDMChannelAsync();

            RuntimeMessage m = new RuntimeMessage(await c.SendMessageAsync(message));
            Log.Message("Sent message to " + user.Username);
            return m;
        }

        public async Task<IDiscordMessage> SendMessage(IDiscordEmbed embed)
        {
            IDMChannel c = await user.GetOrCreateDMChannelAsync();
            IMessage m = await c.SendMessageAsync("", false, (embed as IProxy<EmbedBuilder>).ToNativeObject());
            Log.Message("Sent message to " + user.Username);
            return new RuntimeMessage(m);
        }

        public async Task SetNickname(string text)
        {
            await (user as IGuildUser).ModifyAsync(x =>
            {
                x.Nickname = text;
            });
        }

        public async Task Unban(IDiscordGuild guild)
        {
            IGuild x = (guild as IProxy<IGuild>).ToNativeObject();
            await x.RemoveBanAsync(user);
        }

        public IUser ToNativeObject()
        {
            return user;
        }
    }
}