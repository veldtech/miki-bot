using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Utility
{
    public static class Retry
    {
        public static async Task<T> RetryAsync<T>(Func<Task<T>> func, int delayMs, int maxTries = 5)
        {
            var tries = 0;
            while(tries < maxTries)
            {
                try
                {
                    return await func();
                }
                catch
                {
                    tries++;
                    if(tries >= maxTries)
                    {
                        throw;
                    }
                    await Task.Delay(delayMs);
                }
            }

            throw new InvalidOperationException();
        }
    }
}
