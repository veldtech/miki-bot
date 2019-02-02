using Miki.API.Imageboards.Enums;
using Miki.API.Imageboards.Objects;
using Miki.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.API.Imageboards
{
	public class ImageboardProvider<T> where T : BooruPost
	{
		public ImageboardConfigurations Config = new ImageboardConfigurations();

		public ImageboardProvider(string queryKey)
			: this(new Uri(queryKey))
		{
		}

		public ImageboardProvider(Uri queryKey)
		{
			Config.QueryKey = queryKey;
		}

		public ImageboardProvider(ImageboardConfigurations config)
		{
			Config = config;
		}

		public virtual async Task<T> GetPostAsync(string content, ImageboardRating r)
		{
			bool nsfw = false;
			string[] command = content.Split(' ');

			List<string> tags = new List<string>();

			switch (r)
			{
				case ImageboardRating.EXPLICIT:
				{
					tags.Add(Config.ExplicitTag);
					nsfw = true;
				}
				break;

				case ImageboardRating.QUESTIONABLE:
				{
					tags.Add(Config.QuestionableTag);
					nsfw = true;
				}
				break;

				case ImageboardRating.SAFE:
				{
					tags.Add(Config.SafeTag);
				}
				break;
			}
			tags.AddRange(command);

			if (nsfw)
			{
				RemoveBannedTerms(Config, tags);
				AddBannedTerms(Config, tags);
			}

			string outputTags = GetTags(tags.ToArray());

			using (RestClient c = new RestClient(Config.QueryKey + outputTags))
			{
				if (Config.NetUseCredentials)
				{
					//c.UseDefaultCredentials = true;
					Config.NetHeaders.ForEach(x => c.Headers.Add(x.Item1, x.Item2));
				}

				var b = await c.GetAsync<List<T>>();

				if (b.Success)
				{
					if (b.Data.Any())
					{
						return b.Data[MikiRandom.Next(0, b.Data.Count)];
					}
				}
				return default;
			}
		}

		protected static void AddBannedTerms(ImageboardConfigurations config, List<string> tags)
		{
			config.BlacklistedTags.ForEach(x => tags.Add("-" + x));
		}

		protected static void RemoveBannedTerms(ImageboardConfigurations config, List<string> tags)
		{
			tags.RemoveAll(p => config.BlacklistedTags.Contains(p.ToLower()));
		}

		protected static string GetTags(string[] tags)
		{
			List<string> output = new List<string>();

			for (int i = 0; i < tags.Length; i++)
			{
				if (tags[i] == "awoo")
				{
					output.Add("inubashiri_momiji");
					continue;
				}
				if (tags[i] == "miki")
				{
					output.Add("sf-a2_miki");
					continue;
				}
				if (!string.IsNullOrWhiteSpace(tags[i]))
				{
					output.Add(tags[i]);
				}
			}

			string outputTags = string.Join("+", output);
			outputTags.Remove(outputTags.Length - 1);
			return outputTags;
		}
	}
}