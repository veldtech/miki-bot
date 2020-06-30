using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiScript;
using MiScript.Values;
using MiScript.Values.Functions;

namespace Miki.Modules.CustomCommands.Values
{
    public class ScriptSayFunction : ScriptBaseFunction
    {
        public ScriptEmbedBuilder EmbedBuilder { get; set; }
        
        public StringBuilder Output = new StringBuilder();

        public override Task<IScriptValue> InvokeAsync(
            Context context,
            IScriptValue thisValue,
            IScriptValue[] args,
            CancellationToken token = default)
        {
            if (args.Length == 0)
            {
                return Task.FromResult<IScriptValue>(Null);
            }

            var obj = args[0].ToObject(context);

            if (obj is ScriptEmbedBuilder embedBuilder)
            {
                EmbedBuilder = embedBuilder;
            }
            else
            {
                var value = args.ToCombinedString();
            
                if (!string.IsNullOrEmpty(value))
                {
                    Output.AppendLine(value);
                }
            }

            return Task.FromResult<IScriptValue>(Null);
        }
    }
}