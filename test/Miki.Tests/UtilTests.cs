using System.Threading.Tasks;
using Miki.Utility;
using Xunit;

namespace Miki.Tests
{
    public class UtilTests
    {
        [Theory]
        [InlineData("all", true)]
        [InlineData("*", true)]
        [InlineData("ALL", true)]
        [InlineData("al", false)]
        public void IsAllTest(string query, bool success)
        {
            Assert.Equal(success, Utils.IsAll(query));
        }
    }
}
