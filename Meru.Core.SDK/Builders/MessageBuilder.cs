using System.Text;

namespace IA.SDK.Builders
{
    public class MessageBuilder
    {
        private StringBuilder _builder = new StringBuilder();

        public MessageBuilder AppendText(string text, MessageFormatting formatting = MessageFormatting.Plain, bool newLine = true, bool endWithSpace = false)
        {
            if (string.IsNullOrWhiteSpace(text)) return this;

            text = ApplyFormatting(text, formatting);

            if (endWithSpace) text += " ";

            if (newLine)
            {
                _builder.AppendLine(text);
            }
            else
            {
                _builder.Append(text);
            }

            return this;
        }

        public MessageBuilder NewLine()
        {
            _builder.AppendLine("");
            return this;
        }

        public string Build()
        {
            return _builder.ToString();
        }

        public string BuildWithBlockCode(string language = "markdown")
        {
            return "```" + language + "\n" + _builder + "\n```";
        }

        private string ApplyFormatting(string text, MessageFormatting formatting)
        {
            switch (formatting)
            {
                case MessageFormatting.Bold:
                    return "**" + text + "**";

                case MessageFormatting.BoldItalic:
                    return "**_" + text + "_**";

                case MessageFormatting.BoldItalicUnderlined:
                    return "__**_" + text + "_**__";

                case MessageFormatting.Italic:
                    return "_" + text + "_";

                case MessageFormatting.ItalicUnderlined:
                    return "___" + text + "___";

                case MessageFormatting.Underlined:
                    return "__" + text + "__";

                case MessageFormatting.Code:
                    return "`" + text + "`";

                case MessageFormatting.BlockCode:
                    return "```" + text + "```";

                default:
                    return text;
            }
        }
    }

    public enum MessageFormatting
    {
        Plain,
        Bold,
        Italic,
        Underlined,
        BoldItalic,
        BoldUnderlined,
        ItalicUnderlined,
        BoldItalicUnderlined,
        Code,
        BlockCode
    }
}