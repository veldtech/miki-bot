namespace Miki.Tests
{
    using System;
    using Miki.Localization.Models;

    public class MockResourceManager : IResourceManager
    {
        private readonly Func<string, string> keyFactory;

        public MockResourceManager(Func<string, string> keyFactory)
        {
            this.keyFactory = keyFactory;
        }

        public static MockResourceManager PassThrough => new MockResourceManager(x => x);

        /// <inheritdoc />
        public string GetString(string key)
        {
            return keyFactory(key);
        }
    }
}