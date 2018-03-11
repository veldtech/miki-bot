using Miki.Framework;
using Miki.Common;
using Miki.Common.Interfaces;
using System.Threading.Tasks;

namespace Miki.Accounts
{
    internal class EmojiBar
    {
        public EmojiBarSet ValueOn = new EmojiBarSet();
        public EmojiBarSet ValueOff = new EmojiBarSet();

        public int MaxValue = 99999;

        public int Width = 10;

        public EmojiBar(int value, EmojiBarSet charOn, EmojiBarSet charOff, int width = 10)
        {
            MaxValue = value;
            ValueOn = charOn;
            ValueOff = charOff;
            Width = width;
        }

        public async Task<string> Print(int currentValue, IDiscordMessageChannel c)
        {
            string output = "";

            IDiscordUser u = await c.Guild.GetCurrentUserAsync();
            if (!u.HasPermissions(c, DiscordGuildPermission.UseExternalEmojis))
            {
                return "";
            }

            int iteration = MaxValue / Width;
            int currentIteration = iteration;

            for (int i = 0; i < Width; i++)
            {
                output += (currentValue >= currentIteration) ? ValueOn.GetAppropriateSection(0, Width - 1, i) : ValueOff.GetAppropriateSection(0, Width - 1, i);
                currentIteration += iteration;
            }

            return output;
        }
    }

    internal class EmojiBarSet
    {
        public string Start = "[";
        public string Mid = "o";
        public string End = "]";

        public EmojiBarSet()
        {
        }

        public EmojiBarSet(string start, string mid, string end)
        {
            Start = start;
            Mid = mid;
            End = end;
        }

        public string GetAppropriateSection(int start, int end, int current)
        {
            return (current == start) ? Start : (current == end) ? End : Mid;
        }
    }
}