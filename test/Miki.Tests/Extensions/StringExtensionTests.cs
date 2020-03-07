namespace Miki.Tests.Extensions
{
    using Xunit;

    public class StringExtensionTests
    {
        [Fact]
        public void SplitUntilTakeAllTest()
        {
            var expected = "this is a long string, but not long enough to be truncated.";

            Assert.Equal(
                expected, expected.SplitStringUntil(" ", 1000));
        }

        [Fact]
        public void SplitUntilTakeFiveTest()
        {
            var line = "this is a long string, but not long enough to be truncated.";

            Assert.Equal(
                "this is a long string,", line.SplitStringUntil(" ", 23));
        }

        [Fact]
        public void SplitUntilTakeOneTest()
        {
            var line = "Miki is actually a really cool bot.";

            Assert.Equal(
                "Miki", line.SplitStringUntil(" ", 4));
        }
    }
}
