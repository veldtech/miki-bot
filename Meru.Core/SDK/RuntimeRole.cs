using Discord;
using IA.SDK.Interfaces;

namespace IA.SDK
{
    public class RuntimeRole : IDiscordRole, IProxy<IRole>
    {
        private IRole role;

        public RuntimeRole(IRole role)
        {
            this.role = role;
        }

        public ulong Id
        {
            get
            {
                return role.Id;
            }
        }

        public int Position
        {
            get
            {
                return role.Position;
            }
        }

        public string Mention
        {
            get
            {
                if (role.IsMentionable)
                {
                    return role.Mention;
                }
                return "";
            }
        }

        public string Name
        {
            get
            {
                return role.Name;
            }
        }

        Color IDiscordRole.Color
        {
            get
            {
                return new Color(role.Color.R, role.Color.G, role.Color.B);
            }
        }

		public IRole ToNativeObject()
        {
            return role;
        }
    }
}