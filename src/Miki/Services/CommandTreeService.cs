using Miki.Framework.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Miki.Services
{
    public class CommandTreeService
    {
        private readonly CommandTree commandTree;

        public CommandTreeService(CommandTree commandTree)
        {
            this.commandTree = commandTree;
        }

        private Node GetCommandFromSpecificModule(NodeModule module, string commandName)
        {
            commandName = commandName.ToLowerInvariant();

            var commandNode = module.Children
                            .FirstOrDefault(x => x.Metadata.Identifiers
                            .Any(z => z.ToLowerInvariant() == commandName));

            if(commandNode == null)
            {
                return null;
            }

            return commandNode;
        }

        public Node GetCommandByName(string commandName)
        {
            commandName = commandName.ToLowerInvariant();

            var moduleNode = commandTree.Root.Children
                    .OfType<NodeModule>()
                    .FirstOrDefault(x => x.Children
                        .Any(z => z.Metadata.Identifiers
                            .Any(y=>y.ToLowerInvariant() == commandName)
                        )
                    );

            if(moduleNode == null)
            {
                return null;
            }

            return GetCommandFromSpecificModule(moduleNode, commandName);
        }

        public NodeModule GetModuleByName(string moduleName)
        {
            moduleName = moduleName.ToLowerInvariant();

            var moduleNode = commandTree.Root.Children
                    .OfType<NodeModule>()
                    .FirstOrDefault(x => x.Metadata.Identifiers
                        .Any(z => z.ToLowerInvariant() == moduleName));

            if (moduleNode == null)
            {
                return null;
            }

            return moduleNode;
        }

        public Node GetCommandFromModule(string commandName, string moduleName)
        {
            moduleName = moduleName.ToLowerInvariant();
            commandName = commandName.ToLowerInvariant();

            var moduleNode = commandTree.Root.Children
                    .OfType<NodeModule>()
                    .FirstOrDefault(x => x.Metadata.Identifiers
                        .Any(z=>z.ToLowerInvariant() == moduleName));

            if (moduleNode == null)
            {
                return null;
            }

            return GetCommandFromSpecificModule(moduleNode, commandName);
        }

        public NodeContainer GetTree()
            => commandTree.Root;

        public IEnumerable<NodeModule> GetModules()
            => commandTree.Root.Children.OfType<NodeModule>();
    }
}
