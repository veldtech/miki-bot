using System;

namespace Miki.Attributes
{
    public class EmojiAttribute : Attribute
    {
        public string Emoji { get; private set; }

        public EmojiAttribute(string Emoji)
        {
            this.Emoji = Emoji;
        }
    }
}
