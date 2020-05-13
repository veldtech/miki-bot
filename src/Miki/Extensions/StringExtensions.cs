namespace Miki 
{
    using System;
    using System.Globalization;
    using System.Linq;

    public static class StringExtensions
    {
        public static string AsCode(this object str)
        {
            return $"`{str}`";
        }

        public static string CapitalizeFirst(this string str, CultureInfo culture = null)
        {
            if(string.IsNullOrEmpty(str))
            {
                throw new ArgumentNullException(nameof(str));
            }

            return char.ToUpper(str[0], culture ?? CultureInfo.InvariantCulture)
                   + str.Substring(1);
        }

        public static string SplitStringUntil(this string str, string seperator, int maxCount)
        {
            int maxIndex = 0;
            int currentLength = 0;
            int sepCount = seperator.Length;
            var seperatedString = str.Split(seperator);
            for(int i = 0; i < seperatedString.Length; i++)
            {
                // If an additional join is too long
                if(currentLength + sepCount + seperatedString[i].Length > maxCount)
                {
                    break;
                }

                currentLength += sepCount;
                currentLength += seperatedString[i].Length;
                maxIndex = i;
            }

            return string.Join(seperator, seperatedString.Take(maxIndex + 1));
        }
    }
}
