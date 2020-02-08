namespace Miki.UrbanDictionary
{
    using System.Collections.Generic;
    using Miki.UrbanDictionary.Objects;

    public interface IUrbanDictionaryResponse
    {
        IReadOnlyList<string> Tags { get; }

        string ResultType { get; }

        IReadOnlyList<UrbanDictionaryEntry> List { get; }

        IReadOnlyList<string> Sounds { get; }
	}
}