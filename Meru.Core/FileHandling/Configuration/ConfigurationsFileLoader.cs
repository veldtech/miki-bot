using IA.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IA.FileHandling.Configuration
{
    public class ConfigurationsFileLoader
    {
        private string file = "config.json";
        List<IModule> modules = new List<IModule>();

        private ConfigurationsFileLoader()
        {

        }
        public ConfigurationsFileLoader(string filename)
        {
            file = filename + ".json";
        }

        public async Task SaveConfigurationsToFile(List<IModule> modules)
        {
            
        }
    }
}
