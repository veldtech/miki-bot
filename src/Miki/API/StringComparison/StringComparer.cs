using System;
using System.Collections.Generic;
using System.Linq;

namespace Miki.API.StringComparison
{
	public class StringComparer
	{
		private readonly List<string> allComparableStrings = new List<string>();

		public StringComparer(params string[] stringList)
		{
			allComparableStrings.AddRange(stringList);
		}

		public StringComparer(IEnumerable<string> stringList)
		{
			allComparableStrings = stringList.ToList();
		}

		public StringComparison GetBest(string text)
		{
			List<StringComparison> outputList = CompareToAll(text);
			var best = outputList.OrderBy(x => x.score).First();
			return best;
		}

		public List<StringComparison> CompareToAll(string text)
		{
			List<StringComparison> differenceHeurList = new List<StringComparison>();

			foreach (string c in allComparableStrings)
			{
				int difference = 0;

				for (int i = 0; i < text.Length; i++)
				{
					char typedChar = text.ToLower()[i];
					bool found = false;
					for (int j = 0; j < c.Length; j++)
					{
						char actualChar = c.ToLower()[j];

						if (typedChar == actualChar)
						{
							difference += Math.Abs(i - j);
							found = true;
							break;
						}
					}

					if (!found)
					{
						difference += 15;
					}
				}

				difference += Math.Abs(text.Length - c.Length) * 5;

				differenceHeurList.Add(new StringComparison() { text = c, score = difference, comparedTo = text });
			}

			return differenceHeurList;
		}
	}

	public class StringComparison
	{
		public string text;
		public string comparedTo;
		public int score;

		public override string ToString()
		{
			return $"[text: {text}, score: {score}]";
		}
	}
}