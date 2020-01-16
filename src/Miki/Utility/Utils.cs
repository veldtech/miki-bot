namespace Miki
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Bot.Models;
    using Miki.BunnyCDN;
    using Miki.Cache;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Rest;
    using Miki.Exceptions;
    using Miki.Framework;
    using Miki.Framework.Arguments;
    using Miki.Framework.Commands;
    using Miki.Framework.Language;
    using Miki.Helpers;
    using Miki.Localization;
    using Miki.Localization.Models;
    using System.Linq;
    using Miki.Api.Models;
    using Miki.Services;

    public static class Utils
    {
        public const string EveryonePattern = @"@(everyone|here)";

        public static string EscapeEveryone(string text)
            => Regex.Replace(text, EveryonePattern, "@\u200b$1");

        public static T FromEnum<T>(this string argument, T defaultValue)
            where T : Enum
        {
            if (Enum.TryParse(typeof(T), argument, true, out object result))
            {
                return (T)result;
            }
            return defaultValue;
        }

        public static bool TryFromEnum<T>(this string argument, out T value)
            where T : struct
            => Enum.TryParse(argument ?? "", true, out value);

        public static string ToTimeString(this TimeSpan time, Locale instance, bool minified = false)
        {
            List<TimeValue> t = new List<TimeValue>();
            if (Math.Floor(time.TotalDays) > 0)
            {
                if (Math.Floor(time.TotalDays) > 1)
                {
                    t.Add(new TimeValue(instance.GetString("time_days"), time.Days, minified));
                }
                else
                {
                    t.Add(new TimeValue(instance.GetString("time_days"), time.Days, minified));
                }
            }
            if (time.Hours > 0)
            {
                if (time.Hours > 1)
                {
                    t.Add(new TimeValue(instance.GetString("time_hours"), time.Hours, minified));
                }
                else
                {
                    t.Add(new TimeValue(instance.GetString("time_hour"), time.Hours, minified));
                }
            }
            if (time.Minutes > 0)
            {
                if (time.Minutes > 1)
                {
                    t.Add(new TimeValue(instance.GetString("time_minutes"), time.Minutes, minified));
                }
                else
                {
                    t.Add(new TimeValue(instance.GetString("time_minute"), time.Minutes, minified));
                }
            }

            if (t.Count == 0 || time.Seconds > 0)
            {
                if (time.Seconds > 1)
                {
                    t.Add(new TimeValue(instance.GetString("time_seconds"), time.Seconds, minified));
                }
                else
                {
                    t.Add(new TimeValue(instance.GetString("time_second"), time.Seconds, minified));
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
                        text += $", {instance.GetString("time_and")} " + s[s.Count - 1].ToString();
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

        /// <summary>
        /// Gets the root of an exception
        /// </summary>
        public static Exception GetRootException(this Exception e)
        {
            if(e.InnerException != null)
            {
                return e.InnerException.GetRootException();
            }
            return e;
        }

        public static IDiscordUser GetAuthor(this IContext c)
            => c.GetMessage().Author;

        public static bool IsAll(string input)
            => (input == "all") || (input == "*");

        public static EmbedBuilder ErrorEmbed(this IContext e, string message)
            => new LocalizedEmbedBuilder(e.GetLocale())
                .WithTitle(new IconResource("🚫", "miki_error_message_generic"))
                .SetDescription(message)
                .SetColor(1.0f, 0.0f, 0.0f);

        public static EmbedBuilder ErrorEmbedResource(this IContext e, string resourceId, params object[] args)
            => ErrorEmbed(e, e.GetLocale().GetString(resourceId, args));

        public static EmbedBuilder ErrorEmbedResource(this IContext e, IResource resource)
            => ErrorEmbed(e, resource.Get(e.GetLocale()));

        public static DateTime MinDbValue
            => new DateTime(1755, 1, 1, 0, 0, 0);

        public static DiscordEmbed SuccessEmbed(this IContext e, string message)
            => new EmbedBuilder()
            {
                Title = "✅ " + e.GetLocale().GetString("miki_success_message_generic"),
                Description = message,
                Color = new Color(119, 178, 85)
            }.ToEmbed();
        public static DiscordEmbed SuccessEmbedResource(this IContext e, string resource, params object[] param)
            => SuccessEmbed(e, e.GetLocale().GetString(resource, param));

        public static string RemoveMentions(this string arg, IDiscordGuild guild)
        {
            return Regex.Replace(arg, 
                "<@!?(\\d+)>", 
                m => guild.GetMemberAsync(ulong.Parse(m.Groups[1].Value)).Result.Username, 
                RegexOptions.None);
        }

        public static EmbedBuilder RenderLeaderboards(EmbedBuilder embed, List<LeaderboardsItem> items, int offset)
        {
            for (int i = 0; i < Math.Min(items.Count, 12); i++)
            {
                embed.AddInlineField($"#{offset + i + 1}: " + items[i].Name, $"{items[i].Value:n0}");
            }
            return embed;
        }

        public static async Task SyncAvatarAsync(
            IDiscordUser user, 
            IExtendedCacheClient cache, 
            IUserService context, 
            AmazonS3Client s3Service)
        {
            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = "miki-cdn",
                Key = $"avatars/{user.Id}.png",
                ContentType = "image/png",
                CannedACL = new S3CannedACL("public-read")
            };

            string avatarUrl = user.GetAvatarUrl();

            using (var client = new Net.Http.HttpClient(avatarUrl, true))
            {
                request.InputStream = await client.GetStreamAsync();
            }

            var response = await s3Service.PutObjectAsync(request);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new AvatarSyncException();
            }

            await MikiApp.Instance.Services.GetService<BunnyCDNClient>()
                .PurgeCacheAsync($"https://mikido.b-cdn.net/avatars/{user.Id}.png");

            User u = await context.GetOrCreateUserAsync(user);
            await cache.HashUpsertAsync("avtr:sync", user.Id.ToString(), 1);
            u.AvatarUrl = u.Id.ToString();

            await context.UpdateUserAsync(u);
            await context.SaveAsync();
        }
        public static async Task<bool> HeadAvatarAsync(IDiscordUser user)
        {
            if (user == null)
            {
                return false;
            }

            using var client = new Net.Http.HttpClient($"https://cdn.miki.ai/avatars/{user.Id}.png");
            var response = await client.SendAsync(new System.Net.Http.HttpRequestMessage
            {
                Method = new System.Net.Http.HttpMethod("HEAD"),
            });
            return response.Success;
        }

        public static string TakeAll(this IArgumentPack pack)
        {
            List<string> allItems = new List<string>();
            while (pack.CanTake)
            {
                allItems.Add(pack.Take());
            }
            return string.Join(" ", allItems);
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

        public static long Next(long maxValue)
        {
            return Next(0L, maxValue);
        }

        public static int Next(int maxValue)
        {
            return Next(0, maxValue);
        }

        public static int Roll(int maxValue)
        {
            return Next(0, maxValue) + 1;
        }

        public static long Next(long minValue, long maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException();
            }
            return (long)Math.Floor((minValue + ((double)maxValue - minValue) * NextDouble()));
        }

        public static int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException();
            }

            return (int)Math.Floor((minValue + ((double)maxValue - minValue) * NextDouble()));
        }

        public static T Of<T>(IEnumerable<T> collection) 
            => collection.ElementAt(Next(collection.Count()));

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

        private readonly bool minified;

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