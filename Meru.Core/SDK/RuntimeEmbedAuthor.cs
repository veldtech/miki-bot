using Discord;
using IA.SDK.Interfaces;

namespace IA.SDK
{
    internal class RuntimeEmbedAuthor : IEmbedAuthor, IQuery<RuntimeEmbedAuthor>
    {
        private EmbedAuthorBuilder author;

        public RuntimeEmbedAuthor(EmbedAuthorBuilder author)
        {
            this.author = author;
        }

        public string IconUrl
        {
            get
            {
                return author.IconUrl;
            }

            set
            {
                author.IconUrl = value;
            }
        }

        public string Name
        {
            get
            {
                return author.Name;
            }

            set
            {
                author.Name = value;
            }
        }

        public string Url
        {
            get
            {
                return author.Url;
            }

            set
            {
                author.Url = value;
            }
        }

        #region IQuery<this>

        public RuntimeEmbedAuthor Query(string query)
        {
            string[] cutEmbed = query.Split('}');

            foreach (string x in cutEmbed)
            {
                string[] y = x.Split('{');

                if (y.Length <= 1)
                {
                    continue;
                }

                switch (y[0].ToLower().Trim(' '))
                {
                    case "name":
                        {
                            Name = y[1];
                        }
                        break;

                    case "icon":
                        {
                            IconUrl = y[1].Trim(' ');
                        }
                        break;

                    case "url":
                        {
                            Url = y[1].Trim(' ');
                        }
                        break;
                }
            }

            return this;
        }

        #endregion IQuery<this>

        #region IProxy<EmbedAuthorBuilder>

        public EmbedAuthorBuilder ToNativeObject()
        {
            return author;
        }

        #endregion IProxy<EmbedAuthorBuilder>
    }
}