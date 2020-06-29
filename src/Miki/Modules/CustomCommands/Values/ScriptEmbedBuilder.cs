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

        [Function("image")]
        public ScriptEmbedBuilder Image(string url)
        {
            InnerEmbedBuilder.SetImage(url);
            return this;
        }
    }
}