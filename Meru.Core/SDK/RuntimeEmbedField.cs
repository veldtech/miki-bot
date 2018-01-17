using Discord;
using IA.SDK.Interfaces;

namespace IA.SDK
{
    public class RuntimeEmbedField : IEmbedField, IQuery<RuntimeEmbedField>
    {
        private EmbedFieldBuilder field;

        public RuntimeEmbedField()
        {
            field = new EmbedFieldBuilder();
        }

        public RuntimeEmbedField(IEmbedField f)
        {
            field = new EmbedFieldBuilder();
            field.Name = f.Name;
            field.Value = f.Value;
            field.IsInline = f.IsInline;
        }

        public RuntimeEmbedField(EmbedFieldBuilder f)
        {
            field = f;
        }

        public RuntimeEmbedField(string Name, string Value, bool Isinline = false)
        {
            field = new EmbedFieldBuilder();
            field.Name = Name;
            field.Value = Value;
            field.IsInline = IsInline;
        }

        public bool IsInline
        {
            get
            {
                return field.IsInline;
            }

            set
            {
                field.IsInline = value;
            }
        }

        public string Name
        {
            get
            {
                return field.Name;
            }

            set
            {
                field.Name = value;
            }
        }

        public string Value
        {
            get
            {
                return field.Value.ToString();
            }

            set
            {
                field.Value = value;
            }
        }

        public RuntimeEmbedField Query(string query)
        {
            string[] cutEmbed = query.Split('}');

            foreach (string x in cutEmbed)
            {
                string[] y = x.Split('{');

                if (y.Length < 1)
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

                    case "value":
                        {
                            Value = y[1];
                        }
                        break;

                    case "inline":
                        {
                            IsInline = y[1] == "true" ? true : false;
                        }
                        break;
                }
            }

            return this;
        }
    }
}