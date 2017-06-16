using Discord;
using IA.SDK;
using IA.SDK.Interfaces;
using Miki.Languages;
using System;

namespace Miki
{
    public static class Utils
    {
        public static string ToTimeString(this int seconds)
        {
            TimeSpan time = new TimeSpan(0, 0, 0, (int)seconds, 0);
            return ((Math.Floor(time.TotalDays) > 0) ? (Math.Floor(time.TotalDays) + " day" + ((time.TotalDays > 1) ? "s" : "") + ", ") : "") +
              ((time.Hours > 0) ? (time.Hours + " hour" + ((time.Hours > 1) ? "s" : "") + ", ") : "") +
              ((time.Minutes > 0) ? (time.Minutes + " minutes and ") : "") +
              time.Seconds + " second" + ((time.Seconds > 1) ? "s" : "") + ".\n";
        }
        public static string ToTimeString(this long seconds)
        {
            TimeSpan time = new TimeSpan(0, 0, 0, (int)seconds, 0);
            return ((Math.Floor(time.TotalDays) > 0) ? (Math.Floor(time.TotalDays) + " day" + ((time.TotalDays > 1) ? "s" : "") + ", ") : "") +
              ((time.Hours > 0) ? (time.Hours + " hour" + ((time.Hours > 1) ? "s" : "") + ", ") : "") +
              ((time.Minutes > 0) ? (time.Minutes + " minutes and ") : "") +
              time.Seconds + " second" + ((time.Seconds > 1) ? "s" : "") + ".\n";
        }
        public static string ToTimeString(this TimeSpan time)
        {
            return ((Math.Floor(time.TotalDays) > 0) ? (Math.Floor(time.TotalDays) + " day" + ((time.TotalDays > 1) ? "s" : "") + ", ") : "") +
              ((time.Hours > 0) ? (time.Hours + " hour" + ((time.Hours > 1) ? "s" : "") + ", ") : "") +
              ((time.Minutes > 0) ? (time.Minutes + " minutes and ") : "") +
              time.Seconds + " second" + ((time.Seconds > 1) ? "s" : "") + ".\n";
        }

        public static IDiscordEmbed Embed => new RuntimeEmbed(new EmbedBuilder());

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
}