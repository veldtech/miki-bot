using Discord;
using IA.SDK.Builders;
using IA.SDK.Extensions;
using IA.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace IA.SDK
{
    public class RuntimeEmbed : IDiscordEmbed, IProxy<EmbedBuilder>, IQuery<RuntimeEmbed>
    {
        public EmbedBuilder embed;

        public RuntimeEmbed()
        {
            embed = new EmbedBuilder();
        }

        public RuntimeEmbed(EmbedBuilder e)
        {
            embed = e;
        }

        public List<IEmbedField> Fields
        {
            get
            {
                List<IEmbedField> f = new List<IEmbedField>();
                foreach (EmbedFieldBuilder field in embed.Fields)
                {
                    f.Add(new RuntimeEmbedField(field));
                }
                return f;
            }
        }

        public IEmbedAuthor Author
        {
            get
            {
                if (embed.Author == null) return null;
                return new RuntimeEmbedAuthor(embed.Author);
            }
            set
            {
                embed.Author.Name = value.Name;
                embed.Author.IconUrl = value.IconUrl;
                embed.Author.Url = value.Url;
            }
        }

        public Color Color
        {
            get
            {
                return new Color(embed.Color.Value.R, embed.Color.Value.G, embed.Color.Value.B);
            }

            set
            {
                embed.Color = new Discord.Color(value.R, value.G, value.B);
            }
        }

        public string Description
        {
            get
            {
                return embed.Description;
            }

            set
            {
                embed.Description = value;
            }
        }

        public IEmbedFooter Footer
        {
            get
            {
                if (embed.Footer == null) return null;
                return new RuntimeEmbedFooter(embed.Footer);
            }

            set
            {
                embed.Footer = new RuntimeEmbedFooter(embed.Footer).ToNativeObject();
            }
        }

        public string ImageUrl
        {
            get
            {
                return embed.ImageUrl;
            }

            set
            {
                embed.ImageUrl = value;
            }
        }

        public string Title
        {
            get
            {
                return embed.Title;
            }

            set
            {
                embed.Title = value;
            }
        }

        public string Url
        {
            get
            {
                return embed.Url;
            }

            set
            {
                embed.Url = value;
            }
        }

        public string ThumbnailUrl
        {
            get { return embed.ThumbnailUrl; }
            set { embed.ThumbnailUrl = value; }
        }

        public IDiscordEmbed AddField(Action<IEmbedField> field)
        {
            IEmbedField f = new RuntimeEmbedField("", "");

            field.Invoke(f);

            embed.AddField(x =>
            {
                x.Name = f.Name;
                x.Value = f.Value;
                x.IsInline = f.IsInline;
            });

            return this;
        }

        public IDiscordEmbed AddField(IEmbedField field)
        {
            embed.AddField(x =>
            {
                x.Name = field.Name;
                x.Value = field.Value;
                x.IsInline = field.IsInline;
            });

            return this;
        }

        public IDiscordEmbed AddField(string title, string value)
        {
            embed.AddField(title, value);
            return this;
        }

        public IDiscordEmbed AddInlineField(object title, object value)
        {
            embed.AddInlineField(title.ToString(), value);
            return this;
        }

        public IEmbedAuthor CreateAuthor()
        {
            embed.Author = new EmbedAuthorBuilder();
            return Author;
        }

        public IDiscordEmbed CreateAuthor(string text, string iconUrl, string url)
        {
            embed.Author = new EmbedAuthorBuilder().WithName(text).WithIconUrl(iconUrl).WithUrl(url);
            return this;
        }

        public IEmbedFooter CreateFooter()
        {
            embed.Footer = new EmbedFooterBuilder();
            return Footer;
        }

        public IDiscordEmbed CreateFooter(string text, string iconUrl)
        {
            embed.Footer = new EmbedFooterBuilder().WithText(text).WithIconUrl(iconUrl);
            return this;
        }

        public RuntimeEmbed Query(string embed)
        {
            string[] cutEmbed = embed.Slice();

            foreach (string x in cutEmbed)
            {
                switch (x.Split('{')[0].ToLower().Trim(' '))
                {
                    case "title":
                        {
                            Title = x.Peel();
                        }
                        break;

                    case "description":
                    case "desc":
                        {
                            Description = x.Peel();
                        }
                        break;

                    case "url":
                        {
                            Url = x.Peel();
                        }
                        break;

                    case "imageurl":
                        {
                            ImageUrl = x.Peel();
                        }
                        break;

                    case "color":
                    case "c":
                        {
                            string[] colorSplit = x.Peel().Split(',');
                            Color = new Color(float.Parse(colorSplit[0]), float.Parse(colorSplit[1]), float.Parse(colorSplit[2]));
                        }
                        break;

                    case "author":
                        {
                            Author = (Author as IQuery<RuntimeEmbedAuthor>).Query(x.Peel());
                        }
                        break;

                    case "footer":
                        {
                            Footer = (Footer as IQuery<RuntimeEmbedFooter>).Query(x.Peel());
                        }
                        break;

                    case "field":
                        {
                            RuntimeEmbedField em = new RuntimeEmbedField();
                            AddField((em as IQuery<RuntimeEmbedField>).Query(x.Peel()));
                        }
                        break;
                }
            }

            return this;
        }

        public async Task<IDiscordMessage> SendToChannel(ulong channelId)
        {
            IMessageChannel m = (Bot.instance.Client.GetChannel(channelId) as IMessageChannel);
            if (m as IGuildChannel != null)
            {
                if (!(await (m as IGuildChannel).Guild.GetCurrentUserAsync()).GuildPermissions.EmbedLinks)
                {
                    if (string.IsNullOrWhiteSpace(ImageUrl))
                    {
                        return new RuntimeMessage(await m.SendMessageAsync(ToMessageBuilder().Build(), false));
                    }

                    using (WebClient wc = new WebClient())
                    {
                        byte[] image = wc.DownloadData(ImageUrl);
                        using (MemoryStream ms = new MemoryStream(image))
                        {
                            return new RuntimeMessage(await m.SendFileAsync(ms, ImageUrl, ToMessageBuilder().Build()));
                        }
                    }
                }
            }
            return new RuntimeMessage(await m.SendMessageAsync("", false, embed));
        }

        public async Task<IDiscordMessage> SendToChannel(IDiscordMessageChannel channel)
        {
            return await SendToChannel(channel.Id);
        }

        public async Task<IDiscordMessage> SendToUser(ulong userId)
        {
            IDMChannel channel = await (Bot.instance.Client.GetUser(userId)).GetOrCreateDMChannelAsync();
            return new RuntimeMessage(await channel.SendMessageAsync("", false, embed));
        }

        public async Task<IDiscordMessage> SendToUser(IDiscordUser user)
        {
            return await SendToUser(user.Id);
        }

        public async Task ModifyMessage(IDiscordMessage message)
        {
            IMessageChannel m = ((message.Channel as IProxy<IChannel>).ToNativeObject() as IMessageChannel);
            if (m != null)
            {
                if (!(await (m as IGuildChannel).Guild.GetCurrentUserAsync()).GuildPermissions.EmbedLinks)
                {
                    await message.ModifyAsync(ToMessageBuilder().Build());
                    return;
                }
            }
            await message.ModifyAsync(this);
        }

        public IDiscordEmbed SetAuthor(string name, string imageurl, string url)
        {
            embed.Author = new EmbedAuthorBuilder() { Name = name, IconUrl = imageurl, Url = url };
            return this;
        }

        public IDiscordEmbed SetColor(Color color)
        {
            Color = color;
            return this;
        }

        public IDiscordEmbed SetColor(float r, float g, float b)
        {
            return SetColor(new Color(r, g, b));
        }

        public IDiscordEmbed SetDescription(string description)
        {
            Description = description;
            return this;
        }

        public IDiscordEmbed SetFooter(string text, string iconurl)
        {
            embed.Footer = new EmbedFooterBuilder() { Text = text, IconUrl = iconurl };
            return this;
        }

        public IDiscordEmbed SetImageUrl(string url)
        {
            ImageUrl = url;
            return this;
        }

        public IDiscordEmbed SetThumbnailUrl(string url)
        {
            ThumbnailUrl = url;
            return this;
        }

        public IDiscordEmbed SetTitle(string title)
        {
            Title = title;
            return this;
        }

        public IDiscordEmbed SetUrl(string url)
        {
            Url = url;
            return this;
        }

        public MessageBuilder ToMessageBuilder()
        {
            MessageBuilder b = new MessageBuilder();

            if (Author != null)
            {
                b.AppendText(Author.Name, MessageFormatting.Bold);
            }

            b.AppendText(Title, MessageFormatting.Bold)
             .AppendText(Description);

            foreach (IEmbedField f in Fields)
            {
                b.AppendText(f.Name, MessageFormatting.Underlined)
                 .AppendText(f.Value)
                 .NewLine();
            }

            if (Footer != null)
            {
                b.AppendText(Footer.Text, MessageFormatting.Italic);
            }

            return b;
        }

        public EmbedBuilder ToNativeObject()
        {
            return embed;
        }
    }
}