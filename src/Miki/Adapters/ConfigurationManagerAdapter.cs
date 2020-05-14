using System;
using Microsoft.Extensions.DependencyInjection;
using Miki.Configuration;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Nodes;

namespace Miki.Adapters
{
    class ConfigurationManagerAdapter : ICommandBuildStep
    {
        public NodeModule BuildModule(NodeModule module, IServiceProvider provider) {
            var configurableService = provider.GetRequiredService<ConfigurationManager>();
            configurableService.RegisterType(module.GetType(), module.Instance);
            return module;
        }

        public Node BuildNode(Node module, IServiceProvider provider)
        {
            return module;
        }
    }
}
