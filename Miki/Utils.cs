using Discord;
using Miki.Common;
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
using Miki.Framework.Events;
using Miki.Framework.Extension;
using Microsoft.EntityFrameworkCore;
using Miki.Models;

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

		public static bool IsAll(this ArgObject input, Locale locale = null)
		{
			if (input == null) return false;
			return ((input?.Argument == (locale?.GetString("common_string_all") ?? "all")) || (input?.Argument == "*"));
		}

		public static EmbedBuilder ErrorEmbed(this EventContext e, string message)
			=> new EmbedBuilder()
			{
				Title = $"🚫 {e.Channel.GetLocale().GetString(LocaleTags.ErrorMessageGeneric)}",
				Description = message,
				Color = new Color(255, 0, 0),
			};

		public static EmbedBuilder ErrorEmbedResource(this EventContext e, string resourceId, params object[] args)
			=> ErrorEmbed(e, e.GetResource(resourceId, args));

		public static EmbedBuilder Embed => new EmbedBuilder();

		// TODO: Cache locale
        public static string GetResource(this EventContext c, string m, params object[] o) => new Locale(c.Channel.Id).GetString(m, o);

        public static Locale GetLocale(this IMessageChannel c) => new Locale(c.Id);

        public static DateTime MinDbValue => new DateTime(1755, 1, 1, 0, 0, 0);

        public static Embed SuccessEmbed(Locale locale, string message)
        {
            return new EmbedBuilder()
            {
                Title = locale.GetString(LocaleTags.SuccessMessageGeneric),
                Description = message,
                Color = new Color(0, 255, 0)
            }.Build();
        }

		public static string RemoveMentions(this ArgObject arg, IGuild guild)
		{
			return Regex.Replace(arg.Argument, "<@!?(\\d+)>", (m) =>
			{
				return (guild.GetUserAsync(ulong.Parse(m.Groups[1].Value))).Result.Username;
			}, RegexOptions.None);
		}

		public static string DefaultIfEmpty(this string a, string b)
		{
			return string.IsNullOrEmpty(a) ? b : a;
		}

		public static EmbedBuilder RenderLeaderboards(EmbedBuilder embed, List<LeaderboardsItem> items, int offset)
		{
			for(int i = 0; i < Math.Min(items.Count, 12); i++)
			{
				embed.AddInlineField($"#{offset + i + 1}: " + items[i].Name, string.Format("{0:n0}", items[i].Value));
			}
			return embed;
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
