using IA.SDK.Interfaces;
using System;
using System.Threading.Tasks;

namespace IA.SDK
{
    public class DiscordEmbed : IDiscordEmbed
    {
        public IEmbedAuthor Author
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Color Color
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string Description
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public IEmbedFooter Footer
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string ImageUrl
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string ThumbnailUrl
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string Title
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string Url
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public IDiscordEmbed AddField(IEmbedField field)
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed AddField(Action<IEmbedField> field)
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed AddField(string title, string value)
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed AddInlineField(object title, object value)
        {
            throw new NotImplementedException();
        }

        public IEmbedAuthor CreateAuthor()
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed CreateAuthor(string text, string iconUrl, string url)
        {
            throw new NotImplementedException();
        }

        public void CreateFooter()
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed CreateFooter(string text, string iconUrl)
        {
            throw new NotImplementedException();
        }

        public Task ModifyMessage(IDiscordMessage message)
        {
            throw new NotImplementedException();
        }

        public Task<IDiscordMessage> SendToChannel(ulong channelId)
        {
            throw new NotImplementedException();
        }

        public Task<IDiscordMessage> SendToChannel(IDiscordMessageChannel channel)
        {
            throw new NotImplementedException();
        }

        public Task<IDiscordMessage> SendToUser(ulong userId)
        {
            throw new NotImplementedException();
        }

        public Task<IDiscordMessage> SendToUser(IDiscordUser user)
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed SetAuthor(string name, string imageurl, string url)
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed SetColor(Color color)
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed SetColor(float r, float g, float b)
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed SetDescription(string description)
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed SetFooter(string text, string iconurl)
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed SetImageUrl(string url)
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed SetThumbnailUrl(string url)
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed SetTitle(string title)
        {
            throw new NotImplementedException();
        }

        public IDiscordEmbed SetUrl(string url)
        {
            throw new NotImplementedException();
        }

        IEmbedFooter IDiscordEmbed.CreateFooter()
        {
            throw new NotImplementedException();
        }
    }
}