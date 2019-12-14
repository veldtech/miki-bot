using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Tests
{
    using System.Threading.Tasks;
    using Xunit;

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
