namespace Miki.Accounts
{
	internal class EmojiBar
	{
		public EmojiBarSet ValueOn;
		public EmojiBarSet ValueOff;

		public int MaxValue;

		public int Width;

		public EmojiBar(int value, EmojiBarSet charOn, EmojiBarSet charOff, int width = 10)
		{
			MaxValue = value;
			ValueOn = charOn;
			ValueOff = charOff;
			Width = width;
		}

		public string Print(int currentValue)
		{
			string output = "";

			int iteration = MaxValue / Width;
			int currentIteration = iteration;

			for (int i = 0; i < Width; i++)
			{
				output += (currentValue >= currentIteration)
                    ? ValueOn.GetAppropriateSection(0, Width - 1, i)
                    : ValueOff.GetAppropriateSection(0, Width - 1, i);
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
