using IA.SDK.Interfaces;
using System;

namespace IA.SDK
{
    public class DiscordRole : IDiscordEntity, IDiscordRole
    {
        public virtual Color Color
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual ulong Id
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
                throw new NotImplementedException();
            }
        }

        public virtual string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual int Position
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}