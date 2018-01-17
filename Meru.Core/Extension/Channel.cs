using IA;
using System;
using System.Threading.Tasks;

namespace Discord
{
    public static class ChannelExtension
    {
        /// <summary>
        /// Sends message only if is allowed.
        /// </summary>
        /// <param name="channel">channel</param>
        /// <param name="message">output message</param>
        /// <returns></returns>
        public static async Task<IUserMessage> SendMessage(this IMessageChannel channel, string message)
        {
            try
            {
                IUserMessage m = await channel.SendMessageAsync(message);
                return m;
            }
            catch (Exception e)
            {
                Log.ErrorAt("msg", e.Message);
            }
            return null;
        }

        /// <summary>
        /// Sends message and deletes it after "seconds"
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static async Task<IUserMessage> SendMessageAndDeleteAsync(this IMessageChannel channel, string message, int seconds)
        {
            IUserMessage m = await channel.SendMessage(message);
            if (seconds > 0) await Task.Run(() => DeleteMessage(m, seconds));
            return m;
        }

        private static async Task DeleteMessage(IUserMessage message, int seconds)
        {
            await Task.Delay(seconds * 1000);
            await message.DeleteAsync();
        }
    }
}