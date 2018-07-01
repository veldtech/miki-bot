using Miki.Framework;
using Miki.Common;
using System.Threading.Tasks;
using Miki.Discord.Common;

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

        public async Task<string> Print(int currentValue, IDiscordGuildChannel c)
        {
            string output = "";

            IDiscordGuildUser u = await (await c.GetGuildAsync()).GetSelfAsync();
            if (!(await c.GetPermissionsAsync(u)).HasFlag(GuildPermission.UseExternalEmojis))
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