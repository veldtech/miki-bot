using Discord;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Languages;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Miki
{
    public static class Utils
    {
        public static string ToTimeString(this int seconds)
        {
            TimeSpan time = new TimeSpan(0, 0, 0, (int)seconds, 0);
            return time.ToTimeString();
        }
        public static string ToTimeString(this long seconds)
        {
            TimeSpan time = new TimeSpan(0, 0, 0, (int)seconds, 0);
            return time.ToTimeString();
        }
        public static string ToTimeString(this TimeSpan time)
        {
            List<TimeValue> t = new List<TimeValue>();
            if (Math.Floor(time.TotalDays) > 0)
            {
                if(Math.Floor(time.TotalDays) > 1)
                {
                    t.Add(new TimeValue("days", time.Days));
                }
                else
                {
                    t.Add(new TimeValue("day", time.Days));
                }
            }
            if (time.Hours > 0)
            {
                if (time.Hours > 1)
                {
                    t.Add(new TimeValue("hours", time.Hours));
                }
                else
                {
                    t.Add(new TimeValue("hour", time.Hours));
                }
            }
            if (time.Minutes > 0)
            {
                if (time.Minutes > 1)
                {
                    t.Add(new TimeValue("minutes", time.Minutes));
                }
                else
                {
                    t.Add(new TimeValue("minute", time.Minutes));
                }
            }
            if (time.Seconds > 0)
            {
                if (time.Seconds > 1)
                {
                    t.Add(new TimeValue("seconds", time.Seconds));
                }
                else
                {
                    t.Add(new TimeValue("second", time.Seconds));
                }
            }

            if (t.Count != 0)
            {
                List<string> s = new List<string>();
                foreach(TimeValue v in t)
                {
                    s.Add(v.ToString());
                }

                string text = "";
                if (t.Count > 1)
                {
                    text = string.Join(", ", s.ToArray(), 0, s.Count - 1);
                    text += "and " + s[s.Count - 1].ToString();
                }
                else if(t.Count == 1)
                {
                    text = s[0].ToString();
                }

                return text;
            }
            return "";
        }

        public static bool GetInputBool(this string input)
        {
            return (input.ToLower() == "yes" || input.ToLower() == "1" || input.ToLower() == "on");
        }

        public static IDiscordEmbed ErrorEmbed(this EventContext e, string message) => ErrorEmbed(e.Channel.GetLocale(), message);

        public static string GetResource(this EventContext c, string m, params object[] o) => Locale.GetEntity(c.Channel.Id).GetString(m, o);

        public static Locale GetLocale(this IDiscordMessageChannel c) => Locale.GetEntity(c.Id);

        public static DateTime MinDbValue => new DateTime(1755, 1, 1, 0, 0, 0);

        public static IDiscordEmbed Embed => RunEmbed;
        public static RuntimeEmbed RunEmbed => new RuntimeEmbed(new EmbedBuilder());

        public static IDiscordEmbed ErrorEmbed(Locale locale, string message)
        {
            return new RuntimeEmbed(new EmbedBuilder())
            {
                Title = "🚫 " + locale.GetString(Locale.ErrorMessageGeneric),
                Description = message,
                Color = new IA.SDK.Color(1, 0, 0)
            };
        }
        public static IDiscordEmbed SuccessEmbed(Locale locale, string message)
        {
            return new RuntimeEmbed(new EmbedBuilder())
            {
                Title = locale.GetString(Locale.SuccessMessageGeneric),
                Description = message,
                Color = new IA.SDK.Color(0, 1, 0)
            };
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

        public TimeValue(string i, int v)
        {
            Value = v;
            Identifier = i;
        }

        public override string ToString()
        {
            return Value + " " + Identifier;
        }
    }
}