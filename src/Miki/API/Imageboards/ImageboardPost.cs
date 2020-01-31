namespace Miki.API.Imageboards
{
	using Miki.API.Imageboards.Enums;
	using Miki.API.Imageboards.Objects;
	using Newtonsoft.Json;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
    using Miki.Net.Http;
    using Miki.Utility;

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

		public virtual async Task<T> GetPostAsync(string content, ImageRating r)
		{
			bool nsfw = false;
			string[] command = content.Split(' ');

			List<string> tags = new List<string>();

			switch (r)
			{
				case ImageRating.EXPLICIT:
				{
					tags.Add(Config.ExplicitTag);
					nsfw = true;
				}
				break;

				case ImageRating.QUESTIONABLE:
				{
					tags.Add(Config.QuestionableTag);
					nsfw = true;
				}
				break;

				case ImageRating.SAFE:
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

            using var c = new HttpClient(Config.QueryKey + outputTags);
            if(Config.NetUseCredentials)
            {
                //c.UseDefaultCredentials = true;
                Config.NetHeaders.ForEach(x => c.Headers.Add(x.Item1, x.Item2));
            }

            var request = await c.GetAsync();

            var b = JsonConvert.DeserializeObject<List<T>>(request.Body);

            if(request.Success)
            {
                if(b.Any())
                {
                    return b[MikiRandom.Next(0, b.Count)];
                }
            }
            return default;
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