using System.Threading;
using System.Threading.Tasks;
using Miki.Discord;
using Miki.Framework;
using MiScript;
using MiScript.Exceptions;
using MiScript.Values;
using MiScript.Values.Functions;

namespace Miki.Modules.CustomCommands.Values
{
    public class CreateEmbedFunction : ScriptBaseFunction
    {
        public override Task<IScriptValue> InvokeAsync(Context context, IScriptValue thisValue, IScriptValue[] args, CancellationToken token = default)
        {
            var embedBuilder = new EmbedBuilder();

            if (args.Length > 0)
            {
                embedBuilder.SetDescription(args[0].ToString());
            }

            return Task.FromResult(FromObject(new ScriptEmbedBuilder(embedBuilder)));
        }
    }
}