using Discord;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace IA.Node
{
    public class Node
    {
        public static void Create(string id, string code)
        {
            StreamWriter sw = new StreamWriter(new FileStream(Directory.GetCurrentDirectory() + @"\" + id + ".js", FileMode.CreateNew));
            sw.Write(
                    "if(process.argv.length > 2)" +
                    "{" +
                        "var input = process.argv[2];" +
                        "input = input.replace(/_/g, ' ');" +
                    "}");
            sw.Write(code);
            sw.Flush();
            sw.Dispose();
        }

        public static async Task<string> RunAsync(string id, string args = "")
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = @"C:\Program Files\nodejs\node.exe";
            args = args.Replace(' ', '_');
            start.Arguments = string.Format("{0} {1}", id, args);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            string output = await Task.Run(() => RunProcessAsync(start));
            return output;
        }

        public static string Run(string id, string args)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = @"C:\Program Files\nodejs\node.exe";
            args = args.Replace(' ', '_');
            start.Arguments = string.Format("{0} {1}", id, args);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            string output = RunProcessAsync(start);
            Log.Message("NODE: " + output);
            return output;
        }

        public static void Run(string programName, string args, IMessageChannel channel)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = @"C:\Program Files\nodejs\node.exe";
            args = args.Replace(' ', '_');
            start.Arguments = string.Format("{0} {1}", programName, args);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            RunProcessRealtime(start, programName, channel);
            channel.SendMessage(":white_check_mark: " + programName + ".js successfully ended.").GetAwaiter().GetResult();
        }

        private static string RunProcessAsync(ProcessStartInfo p)
        {
            if (File.Exists(Directory.GetCurrentDirectory() + @"\" + p.Arguments.Split(' ')[0] + ".js"))
            {
                Process process = Process.Start(p);
                process.Start();
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                return output != "" ? output : ":white_check_mark:";
            }
            else
            {
                Console.WriteLine(Directory.GetCurrentDirectory() + @"\" + p.Arguments.Split(' ')[0] + ".js");
                return ":no_entry_sign: Node '" + p.Arguments.Split(' ')[0] + ".js'not found.";
            }
        }

        private static void RunProcessRealtime(ProcessStartInfo p, string programName, IMessageChannel channel)
        {
            if (File.Exists(Directory.GetCurrentDirectory() + @"\" + p.Arguments.Split(' ')[0] + ".js"))
            {
                Process process = Process.Start(p);
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += async (s, e) =>
                {
                    await channel.SendMessage("[" + programName + "] " + e.Data);
                };
                process.BeginOutputReadLine();
                process.WaitForExit();
                process.Start();
            }
        }
    }
}