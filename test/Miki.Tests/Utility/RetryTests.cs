namespace Miki.Tests.Utility
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Miki.Utility;
    using Xunit;

    public class RetryTests
    {
        [Fact]
        public async Task RetrySuccessTestAsync()
        {
            var resultString = await Retry.RetryAsync(() => Task.FromResult("test"), 1000);
            Assert.Equal("test", resultString);
        }

        [Fact]
        public async Task RetryFailedTestAsync()
        {
            var tries = 0;
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Retry.RetryAsync<string>(() =>
                {
                    tries++;
                    throw new InvalidOperationException();
                }, 1000));
            Assert.Equal(5, tries);
        }
    }
}
