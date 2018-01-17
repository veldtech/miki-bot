using IA.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IA
{
    internal class Manager : IpcSocket
    {
        private AppDomain app = AppDomain.CurrentDomain;
        private Bot bot = null;

        private List<Shard> shard = new List<Shard>();

        private int shardCount;

        public Manager(int shard_count, Bot bot)
        {
            shardCount = shard_count;

            this.bot = bot;
            OpenManager().GetAwaiter().GetResult();
        }

        private async Task Heartbeat()
        {
            for (int i = 0; i < shard.Count; i++)
            {
                if (!shard[i].shardProcess.Responding)
                {
                    Log.Error("[Shard " + i + "] has stopped responding.");
                    shard[i].shardProcess.Kill();
                    shard[i] = new Shard(i, bot);
                }

                if (shard[i].shardProcess.HasExited)
                {
                    Log.Error("[Shard " + i + "] has crashed.");
                    shard[i] = new Shard(i, bot);
                }
            }

            await Task.Delay(1000);
            await Heartbeat();
        }

        private async Task OpenManager()
        {
            app.ProcessExit += App_ProcessExit;

            Process[] p = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            foreach (Process px in p)
            {
                if (px.Id != Process.GetCurrentProcess().Id)
                {
                    px.Kill();
                }
            }

            for (int i = 0; i < shardCount; i++)
            {
                shard.Add(new Shard(i, bot));
            }
            await Task.Run(async () => await Heartbeat());
            await Task.Delay(-1);
        }

        #region events

        private void App_ProcessExit(object sender, EventArgs e)
        {
            foreach (Shard b in shard)
            {
                b.shardProcess.Kill();
            }
        }

        #endregion events
    }
}