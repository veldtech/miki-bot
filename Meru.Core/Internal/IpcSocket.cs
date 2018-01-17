using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace IA.Internal
{
    public delegate void IpcMessageRecieved(string message);

    public class IpcSocket
    {
        public static event IpcMessageRecieved ipcMessageRecieved;

        public async Task SendAsync(Process p, string value)
        {
            using (AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
            {
                try
                {
                    Console.WriteLine("[SERVER] Setting ReadMode to \"Message\".");
                    pipeServer.ReadMode = PipeTransmissionMode.Message;
                }
                catch (NotSupportedException e)
                {
                    Console.WriteLine("[SERVER] Exception:\n    {0}", e.Message);
                }

                Console.WriteLine("[SERVER] Current TransmissionMode: {0}.",
                    pipeServer.TransmissionMode);

                Console.WriteLine(p.StartInfo.Arguments +
                    pipeServer.GetClientHandleAsString());

                pipeServer.DisposeLocalCopyOfClientHandle();

                try
                {
                    using (StreamWriter sw = new StreamWriter(pipeServer))
                    {
                        sw.AutoFlush = true;
                        await sw.WriteLineAsync("SYNC");
                        pipeServer.WaitForPipeDrain();
                        await sw.WriteLineAsync(value);
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("[SERVER] Error: {0}", e.Message);
                }
            }
        }

        public async Task RecieveAsync(string handle)
        {
            using (PipeStream pipeClient = new AnonymousPipeClientStream(PipeDirection.In, handle))
            {
                Console.WriteLine("[CLIENT] Current TransmissionMode: {0}.",
                   pipeClient.TransmissionMode);

                using (StreamReader sr = new StreamReader(pipeClient))
                {
                    string temp;

                    do
                    {
                        Console.WriteLine("[CLIENT] Wait for sync...");
                        temp = await sr.ReadLineAsync();
                    }
                    while (!temp.StartsWith("SYNC"));

                    while ((temp = await sr.ReadLineAsync()) != null)
                    {
                        ipcMessageRecieved.Invoke(temp);
                    }
                }
            }
        }
    }
}