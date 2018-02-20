using Discord;
using Miki.Common;
using Miki.Common.Events;
using Miki.Common.Interfaces;
using Miki;
using Miki.Accounts;
using Miki.API.Leaderboards;
using Miki.Languages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Miki
{
    public static class Utils   
    {
        public static DateTime UnixToDateTime(long unix)
        {
            DateTime time = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            time = time.AddSeconds(unix).ToLocalTime();
            return time;
        }

        public static string ToTimeString(this int seconds, Locale localized, bool minified = false)
        {
            TimeSpan time = new TimeSpan(0, 0, 0, seconds, 0);
            return time.ToTimeString(localized, minified);
        }
        public static string ToTimeString(this float seconds, Locale localized, bool minified = false)
        {
            TimeSpan time = new TimeSpan(0, 0, 0, (int)seconds, 0);
            return time.ToTimeString(localized, minified);
        }
        public static string ToTimeString(this long seconds, Locale localized, bool minified = false)
        {
            TimeSpan time = new TimeSpan(0, 0, 0, (int)seconds, 0);
            return time.ToTimeString(localized, minified);
        }

		// Make this prettier
        public static string ToTimeString(this TimeSpan time, Locale localized, bool minified = false)
        {
            List<TimeValue> t = new List<TimeValue>();
            if (Math.Floor(time.TotalDays) > 0)
            {
                if (Math.Floor(time.TotalDays) > 1)
                {
                    t.Add(new TimeValue(localized.GetString("time_days"), time.Days, minified));
                }
                else
                {
                    t.Add(new TimeValue(localized.GetString("time_days"), time.Days, minified));
                }
            }
            if (time.Hours > 0)
            {
                if (time.Hours > 1)
                {
                    t.Add(new TimeValue(localized.GetString("time_hours"), time.Hours, minified));
                }
                else
                {
                    t.Add(new TimeValue(localized.GetString("time_hour"), time.Hours, minified));
                }
            }
            if (time.Minutes > 0)
            {
                if (time.Minutes > 1)
                {
                    t.Add(new TimeValue(localized.GetString("time_minutes"), time.Minutes, minified));
                }
                else
                {
                    t.Add(new TimeValue(localized.GetString("time_minute"), time.Minutes, minified));
                }
            }
            if (time.Seconds > 0)
            {
                if (time.Seconds > 1)
                {
                    t.Add(new TimeValue(localized.GetString("time_seconds"), time.Seconds, minified));
                }
                else
                {
                    t.Add(new TimeValue(localized.GetString("time_second"), time.Seconds, minified));
                }
            }

            if (t.Count != 0)
            {
                List<string> s = new List<string>();
                foreach (TimeValue v in t)
                {
                    s.Add(v.ToString());
                }

                string text = "";
                if (t.Count > 1)
                {
                    int offset = 1;
                    if (minified)
                    {
                        offset = 0;
                    }
                    text = string.Join(", ", s.ToArray(), 0, s.Count - offset);

                    if (!minified)
                    {
                        text += $", {localized.GetString("time_and")} " + s[s.Count - 1].ToString();
                    }
                }
                else if (t.Count == 1)
                {
                    text = s[0].ToString();
                }

                return text;
            }
            return "";
        }

        public static float FromHoursToSeconds(this float value)
        {
            return (float)Math.Round(value * 60 * 60);
        }

		public static bool IsAll(string input, Locale locale = null)
		{
			return ((input == locale?.GetString("common_string_all")) || (input == "*"));
		}
		public static bool ToBool(this string input)
        {
            return (input.ToLower() == "yes" || input.ToLower() == "1" || input.ToLower() == "on");
        }

        public static async Task ForEachAsync<T>(this IEnumerable<T> list, Func<T, Task> func)
        {
            foreach(T x in list)
            {
                await func(x);
            }
        }

		public static IDiscordEmbed ErrorEmbed(this EventContext e, string message)
			=> Embed.SetTitle($"🚫 {e.Channel.GetLocale().GetString(LocaleTags.ErrorMessageGeneric)}")
			.SetDescription(message)
			.SetColor(1.0f, 0.0f, 0.0f);
		public static IDiscordEmbed ErrorEmbedResource(this EventContext e, string resourceId, params object[] args)
			=> ErrorEmbed(e, e.GetResource(resourceId, args));


		// TODO: Cache locale
        public static string GetResource(this EventContext c, string m, params object[] o) => new Locale(c.Channel.Id).GetString(m, o);

        public static Locale GetLocale(this IDiscordMessageChannel c) => new Locale(c.Id);

        public static DateTime MinDbValue => new DateTime(1755, 1, 1, 0, 0, 0);

        public static IDiscordEmbed Embed => new RuntimeEmbed(new EmbedBuilder());

        public static IDiscordEmbed SuccessEmbed(Locale locale, string message)
        {
            return new RuntimeEmbed(new EmbedBuilder())
            {
                Title = locale.GetString(LocaleTags.SuccessMessageGeneric),
                Description = message,
                Color = new Common.Color(0, 1, 0)
            };
        }

		public static string DefaultIfEmpty(this string a, string b)
		{
			return string.IsNullOrEmpty(a) ? b : a;
		}

		public static IDiscordEmbed RenderLeaderboards(IDiscordEmbed embed, List<LeaderboardsItem> items, int offset)
		{
			for(int i = 0; i < Math.Min(items.Count, 12); i++)
			{
				embed.AddInlineField($"#{offset + i + 1}: " + items[i].Name, string.Format("{0:n0}", items[i].Value));
			}
			return embed;
		}
    }

	public class Args
	{
		List<string> args;

		public int Count => args.Count;

		public Args(string a)
		{
			args = new List<string>();
			args.AddRange(a.Split(' '));
			args.RemoveAll(x => string.IsNullOrEmpty(x));
		}

		public bool Exists(string arg)
		{
			return args.Contains(arg);
		}

		public ArgObject First()
			=> Get(0);

		public ArgObject Get(int index)
		{
			index = Math.Clamp(index, 0, args.Count);

			if (index >= args.Count)
				return null;

			return new ArgObject(args[index], index, this);
		}

		public ArgObject Join()
			=> new ArgObject(string.Join(" ", args), 0, this); 

		public void Remove(string value)
		{
			args.Remove(value);
		}
	}

	public class ArgObject
	{
		public string Argument { get; private set; }

		Args args;

		int index;

		public bool IsLast
			=> (args.Count - 1 == index);

		public bool IsMention
			=> Regex.IsMatch(Argument, "<@(!?)\\d+>");

		public ArgObject(string argument, int index, Args a)
		{
			Argument = argument;
			this.index = index;
			args = a;
		}

		public int AsInt(int defaultValue = 0)
		{
			if (int.TryParse(Argument, out int s))
			{
				return s;
			}
			return defaultValue;
		}

		public async Task<IDiscordUser> GetUserAsync(IDiscordGuild guild)
		{
			if(IsMention)
			{
				return await guild.GetUserAsync(ulong.Parse(Argument
					.TrimStart('<')
					.TrimStart('@')
					.TrimStart('!')
					.TrimEnd('>')));
			}
			else if(ulong.TryParse(Argument, out ulong id))
			{
				return await guild.GetUserAsync(id);
			}
			return await guild.GetUserAsync(Argument);
		}

		public ArgObject Next()
		{
			if (IsLast)
				return null;

			return args.Get(index + 1);
		}
	}

    public class MikiRandom : RandomNumberGenerator
    {
        private static readonly RandomNumberGenerator rng = new RNGCryptoServiceProvider();

        public static int Next()
        {
            var data = new byte[sizeof(int)];
            rng.GetBytes(data);
            return BitConverter.ToInt32(data, 0) & (int.MaxValue - 1);
        }

        public static int Next(int maxValue)
        {
            return Next(0, maxValue);
        }

		public static int Roll(int maxValue)
		{
			return Next(0, maxValue) + 1;
		}

        public static int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException();
            }
            return (int)Math.Floor((minValue + ((double)maxValue - minValue) * NextDouble()));
        }

        public static double NextDouble()
        {
            var data = new byte[sizeof(uint)];
            rng.GetBytes(data);
            var randUint = BitConverter.ToUInt32(data, 0);
            return randUint / (uint.MaxValue + 1.0);
        }

        public override void GetBytes(byte[] data)
        {
            rng.GetBytes(data);
        }

        public override void GetNonZeroBytes(byte[] data)
        {
            rng.GetNonZeroBytes(data);
        }
    }

    public class TimeValue
    {
        public int Value { get; set; }
        public string Identifier { get; set; }

        private bool minified;

        public TimeValue(string i, int v, bool minified = false)
        {
            Value = v;
            if (minified)
            {
                Identifier = i[0].ToString();
            }
            else
            {
                Identifier = i;
            }
            this.minified = minified;
        }

        public override string ToString()
        {
            if (minified) return Value + Identifier;
            return Value + " " + Identifier;
        }
    }
}
