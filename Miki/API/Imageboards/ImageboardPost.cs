using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Miki.Framework;
using Miki.API.Imageboards.Enums;
using Miki.API.Imageboards.Interfaces;
using Miki.API.Imageboards.Objects;
using Newtonsoft.Json;

namespace Miki.API.Imageboards
{
    public class ImageboardProvider<T> where T : BooruPost
    {
        public ImageboardConfigurations Config = new ImageboardConfigurations();

        public ImageboardProvider(string queryKey)
        {
            Config.QueryKey = queryKey;
        }
        public ImageboardProvider(ImageboardConfigurations config)
        {
            Config = config;
        }

        public virtual T GetPost(string content, ImageboardRating r)
        {
            WebClient c = new WebClient();
            byte[] b;
            bool nsfw = false;
            string[] command = content.Split(' ');

            List<string> tags = new List<string>();

            if (Config.NetUseCredentials)
            {
                c.UseDefaultCredentials = true;
                c.Credentials = Config.NetCredentials;
                Config.NetHeaders.ForEach(x => c.Headers.Add(x));
            }

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

            b = c.DownloadData(Config.QueryKey + outputTags);
            if (b != null)
            {
                string result = Encoding.UTF8.GetString(b);

				JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
				List<T> d = JsonConvert.DeserializeObject<List<T>>(result, settings);

				if (d != null)
				{
					if (d.Any())
					{
						return d[MikiRandom.Next(0, d.Count)];
					}
				}
            }
            return default(T);
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
