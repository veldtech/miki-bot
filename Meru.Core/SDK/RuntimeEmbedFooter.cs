using Discord;
using IA.SDK.Interfaces;

namespace IA.SDK
{
    public class RuntimeEmbedFooter : IEmbedFooter, IProxy<EmbedFooterBuilder>, IQuery<RuntimeEmbedFooter>
    {
        private EmbedFooterBuilder footer;

        public RuntimeEmbedFooter(EmbedFooterBuilder footer)
        {
            this.footer = footer;
        }

        public string IconUrl
        {
            get
            {
                return footer.IconUrl;
            }

            set
            {
                footer.IconUrl = value;
            }
        }

        public string Text
        {
            get
            {
                return footer.Text;
            }

            set
            {
                footer.Text = value;
            }
        }

        #region IQuery<this>

        public RuntimeEmbedFooter Query(string query)
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
                    case "text":
                        {
                            Text = y[1];
                        }
                        break;

                    case "icon":
                        {
                            IconUrl = y[1];
                        }
                        break;
                }
            }

            return this;
        }

        #endregion IQuery<this>

        #region IProxy<EmbedFooterBuilder>

        public EmbedFooterBuilder ToNativeObject()
        {
            return footer;
        }

        #endregion IProxy<EmbedFooterBuilder>
    }
}