using System;
using System.Threading.Tasks;
using Miki.Utility;
using Xunit;

namespace Miki.Tests
{
    public static class Assext
    {
        public static async Task ThrowsRootAsync<T>(Func<Task> t)
        {
            try
            {
                await t();
                Assert.True(false, $"Function did not throw {typeof(T).FullName}.");
            }
            catch(Exception e)
            {
                Assert.IsType<T>(e.GetRootException());
            }
        }
    }
}
