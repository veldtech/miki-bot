using System.Drawing;
using System.Threading.Tasks;
using Miki.Discord;
using MiScript.Attributes;
using MiScript.Exceptions;

namespace Miki.Modules.CustomCommands.Values
{
    public class ScriptEmbedBuilder
    {
        public ScriptEmbedBuilder(EmbedBuilder innerEmbedBuilder)
        {
            InnerEmbedBuilder = innerEmbedBuilder;
        }
        
        public EmbedBuilder InnerEmbedBuilder { get; }

        [Function("title")]
        public ScriptEmbedBuilder Title(string title)
        {
            InnerEmbedBuilder.SetTitle(title);
            return this;
        }

        [Function("footer")]
        public ScriptEmbedBuilder Footer(string text, string url = null)
        {
            InnerEmbedBuilder.SetFooter(text, url ?? "");
            return this;
        }

        [Function("image")]
        public ScriptEmbedBuilder Image(string url)
        {
            InnerEmbedBuilder.SetImage(url);
            return this;
        }

        [Function("field")]
        public ScriptEmbedBuilder Field(string title, string content)
        {
            if (string.IsNullOrEmpty(content)) throw new MiScriptException("Parameter content is required");
            
            InnerEmbedBuilder.AddField(title, content);
            return this;
        }

        [Function("inlineField")]
        public ScriptEmbedBuilder InlineField(string title, string content)
        {
            if (string.IsNullOrEmpty(content)) throw new MiScriptException("Parameter content is required");
            
            InnerEmbedBuilder.AddInlineField(title, content);
            return this;
        }

        [Function("author")]
        public ScriptEmbedBuilder Author(string name, string image = null, string url = null)
        {
            InnerEmbedBuilder.SetAuthor(name, image, url);
            return this;
        }

        [Function("color")]
        public ScriptEmbedBuilder Color(string htmlColor)
        {
            try
            {
                var color = ColorTranslator.FromHtml(htmlColor);
                InnerEmbedBuilder.SetColor(color.R, color.G, color.B);
                return this;
            }
            catch
            {
                throw new MiScriptException("Parameter color is incorrect");
            }
        }
    }
}