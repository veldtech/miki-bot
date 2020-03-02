namespace Miki 
{
    using System.Linq;

    public static class StringExtensions
    {
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
