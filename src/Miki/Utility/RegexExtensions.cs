using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Utility
{
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public static class RegexExtensions
    {
        public static async ValueTask<string> ReplaceAsync(this Regex regex, string input, Func<Match, ValueTask<string>> replacementFn)
        {
            var sb = new StringBuilder();
            var lastIndex = 0;

            foreach(Match match in regex.Matches(input))
            {
                sb.Append(input, lastIndex, match.Index - lastIndex)
                    .Append(await replacementFn(match).ConfigureAwait(false));

                lastIndex = match.Index + match.Length;
            }

            sb.Append(input, lastIndex, input.Length - lastIndex);
            return sb.ToString();
        }
    }
}
