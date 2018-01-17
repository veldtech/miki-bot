using IA.Events;
using IA.SDK;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace IA.Addons
{
    public class AddonManager
    {
        public string CurrentDirectory { get; private set; } = Directory.GetCurrentDirectory() + "/modules/";

        /// <summary>
        /// Loads addons in ./modules folder
        /// </summary>
        public async Task Load(Bot bot)
        {
            if (!Directory.Exists(CurrentDirectory) || Directory.GetFiles(CurrentDirectory).Length == 0)
            {
                Log.Warning("No modules found, ignoring...");
                Directory.CreateDirectory(CurrentDirectory);
                return;
            }

            string[] allFiles = Directory.GetFiles(CurrentDirectory);

            foreach (string s in allFiles)
            {
                string newS = s.Split('/')[s.Split('/').Length - 1];
                newS = newS.Remove(newS.Length - 4);

                try
                {
                    if (!s.EndsWith(".dll"))
                    {
                        continue;
                    }

                    Assembly addon = Assembly.Load(File.ReadAllBytes(s));

                    if (addon.CreateInstance(newS + ".Addon") is IAddon currentAddon)
                    {
                        IAddonInstance aInstance = new RuntimeAddonInstance();
                        aInstance = await currentAddon.Create(aInstance);

                        foreach (IModule nm in aInstance.Modules)
                        {
                            IModule newModule = new RuntimeModule(nm);
                            await newModule.InstallAsync(bot);
                        }
                        Log.Done($"loaded Add-On {newS} successfully");
                    }
                }
                catch(Exception ex)
                {
                }
            }
        }

        public async Task LoadSpecific(Bot bot, string module)
        {
            string s = CurrentDirectory + (module.EndsWith(".dll") ? module : module + ".dll");

            Assembly addon = Assembly.Load(File.ReadAllBytes(s));

            string newS = s.Split('/')[s.Split('/').Length - 1];
            newS = newS.Remove(newS.Length - 4);

            if (addon.CreateInstance(newS + ".Addon") is IAddon currentAddon)
            {
                RuntimeAddonInstance aInstance = new RuntimeAddonInstance();
                aInstance = new RuntimeAddonInstance(await currentAddon.Create(aInstance), bot);

                foreach (IModule nm in aInstance.Modules)
                {
                    if (bot.Events.GetModuleByName(nm.Name) != null)
                    {
                        Log.Warning("Module already loaded, stopping load");
                        return;
                    }
                    RuntimeModule newModule = new RuntimeModule(nm);
                    await newModule.InstallAsync(bot);
                }

                Log.Done($"Loaded Add-On \"{newS}\" successfully");
            }
            else
            {
                Log.Error($"failed to reload module \"{newS}\"");
            }
        }

        public async Task Reload(Bot bot, string module)
        {
            string s = CurrentDirectory + module + ".dll";

            Assembly addon = Assembly.Load(File.ReadAllBytes(s));

            string newS = s.Split('/')[s.Split('/').Length - 1];
            newS = newS.Remove(newS.Length - 4);

            if (addon.CreateInstance(newS + ".Addon") is IAddon currentAddon)
            {
                RuntimeAddonInstance aInstance = new RuntimeAddonInstance();
                aInstance = new RuntimeAddonInstance(await currentAddon.Create(aInstance), bot);

                foreach (IModule nm in aInstance.Modules)
                {
                    await bot.Events.GetModuleByName(nm.Name).UninstallAsync(bot);

                    RuntimeModule newModule = nm as RuntimeModule;
                    await newModule.InstallAsync(bot);
                }
                Log.Done($"Reloaded Add-On \"{newS}\" successfully");
            }
            else
            {
                Log.Error($"failed to reload module \"{newS}\"");
            }
        }

        public async Task Unload(Bot bot, string module)
        {
            string s = CurrentDirectory + module + ".dll";

            Assembly addon = Assembly.Load(File.ReadAllBytes(s));

            string newS = s.Split('/')[s.Split('/').Length - 1];
            newS = newS.Remove(newS.Length - 4);

            if (addon.CreateInstance(newS + ".Addon") is IAddon currentAddon)
            {
                RuntimeAddonInstance aInstance = new RuntimeAddonInstance();
                aInstance = new RuntimeAddonInstance(await currentAddon.Create(aInstance), bot);

                foreach (IModule nm in aInstance.Modules)
                {
                    IModule mod = bot.Events.GetModuleByName(nm.Name);

                    if (mod != null)
                    {
                        await mod.UninstallAsync(bot);
                    }
                }
                Log.Done($"Unloaded Add-On \"{newS}\" successfully");
            }
            else
            {
                Log.Error($"failed to unload module \"{newS}\"");
            }
        }
    }
}