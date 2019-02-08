using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using MiScript;
using MiScript.Models;
using MiScript.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.CustomCommands
{
    [Module("CustomCommands")]
    public class CustomCommandsModule
    {
        private Tokenizer _tokenizer = new Tokenizer();

        [Command(Name = "newcommand", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task NewCustomCommandAsync(EventContext e)
        {
            if(e.Arguments.Take(out string commandName))
            {
                if(commandName.Contains(' '))
                {
                    throw new InvalidCharacterException(" ");
                }

                if(!e.Arguments.CanTake)
                {
                    // TODO (Veld): Command has no function body.
                    return;
                }

                string scriptBody = e.Arguments.Pack.TakeAll().TrimStart('`').TrimEnd('`');

                try
                {
                    var tokens = _tokenizer.Tokenize(scriptBody);
                   
                    string response = new Parser(tokens).Parse(new Dictionary<string, object>
                    {
                        { "author", e.Author.Username },
                        { "author.id", e.Author.Id }
                    });

                    response = response.Replace("@everyone", "ping :3");
                    response = response.Replace("@here", "ping! :(");

                    if(response != null)
                    {
                        e.Channel.QueueMessage(response);
                    }
                }
                catch(Exception ex)
                {
                    await e.ErrorEmbed($"An error occurred when parsing your script: ```{ex.ToString()}```")
                        .ToEmbed().QueueToChannelAsync(e.Channel);
                }
            }
        }
    }
}
