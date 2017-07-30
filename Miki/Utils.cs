using Discord;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Languages;
using System;
using System.Collections.Generic;

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

    public class MikiRandom
    {
        private static readonly Random getrandom = new Random();
        private static readonly object syncLock = new object();
            
        public static int GetRandomNumber(int max)
        {
            lock (syncLock)
            {
                return getrandom.Next(0, max);
            }
        }
        public static int GetRandomNumber(int min, int max)
        {
            lock (syncLock)
            {
                return getrandom.Next(min, max);
            }
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