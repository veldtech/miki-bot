using IA.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace IA
{
    internal class Shard : IpcSocket
    {
        public int id;
        public Process shardProcess;

        /// <summary>
        /// Create a new shard
        /// </summary>
        /// <param name="shard_id">shard id</param>
        public Shard(int shard_id, Bot bot)
        {
            Log.Message("Starting shard " + shard_id);
            id = shard_id;
            OpenShardAsync(bot).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Opens a new shard.
        /// </summary>
        public async Task OpenShardAsync(Bot bot)
        {
            ProcessStartInfo info = new ProcessStartInfo()
            {
                Arguments = id.ToString(),
                WorkingDirectory = Directory.GetCurrentDirectory(),
                FileName = $"{bot.Name}.exe",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            shardProcess = Process.Start(info);
            shardProcess.EnableRaisingEvents = true;
            shardProcess.OutputDataReceived += (s, e) =>
            {
                Log.Print("[shard-" + id + "] " + e.Data);
            };
            shardProcess.ErrorDataReceived += (s, e) =>
            {
                Log.Print("[err@shard-" + id + "] " + e.Data, ConsoleColor.Red, LogLevel.ERROR);
            };
            shardProcess.BeginOutputReadLine();
            shardProcess.BeginErrorReadLine();

            await Task.Delay(5000);
        }
    }
}