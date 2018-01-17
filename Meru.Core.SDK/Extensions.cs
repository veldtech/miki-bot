using IA.SDK.Interfaces;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IA.SDK.Extensions
{
    public static class ExtensionIDiscordMessage
    {
        public static string RemoveMentions(this IDiscordMessage message, string content = "")
        {
            if (content == "")
            {
                content = message.Content;
            }

            foreach (ulong id in message.MentionedUserIds)
			{
				content = content.Replace($"<@!{id.ToString()}>", message.Guild.GetUserAsync(id).GetAwaiter().GetResult().Nickname);
				content = content.Replace($"<@{id.ToString()}>", message.Guild.GetUserAsync(id).GetAwaiter().GetResult().Username);
            }
            foreach (ulong id in message.MentionedRoleIds)
            {
                content = content.Replace($"<@&{id.ToString()}>", message.Guild.GetRole(id).Name);
            }
            foreach (ulong id in message.MentionedChannelIds)
            {
                content = content.Replace($"<#{id.ToString()}>", message.Guild.GetChannels().GetAwaiter().GetResult().Find(c => { return c.Id == id; }).Name);
            }
            return content;
        }
    }

    public static class ExtensionString
    {
        public static string Peel(this string embed, char startChar = '{', char endChar = '}')
        {
            int shouldCut = 0;
            int lastCutPosition = 0;
            List<string> cutEmbed = new List<string>();

            for (int i = 0; i < embed.Length; i++)
            {
                char x = embed[i];

                if (x == startChar)
                {
                    shouldCut++;
                    if (shouldCut == 1)
                    {
                        lastCutPosition = i + 1;
                    }
                }

                if (x == endChar)
                {
                    shouldCut--;
                    if (shouldCut == 0)
                    {
                        string tempStr = embed.Substring(lastCutPosition);
                        tempStr = tempStr.Remove((i - lastCutPosition));
                        cutEmbed.Add(tempStr);
                        lastCutPosition = i;
                    }
                }
            }
            return string.Join(" ", cutEmbed);
        }

        public static string[] Slice(this string embed, char startChar = '{', char endChar = '}')
        {
            int shouldCut = 0;
            int lastCutPosition = 0;

            embed = embed.Replace(" ", "");

            List<string> cutEmbed = new List<string>();

            for (int i = 0; i < embed.Length; i++)
            {
                char x = embed[i];
                if (x == ' ')
                {
                    if (shouldCut == 0)
                    {
                        lastCutPosition = i;
                    }
                }

                if (x == startChar)
                {
                    shouldCut++;
                }

                if (x == endChar)
                {
                    shouldCut--;
                    if (shouldCut == 0)
                    {
                        string tempStr = embed.Substring(lastCutPosition != 0 ? lastCutPosition + 1 : lastCutPosition);
                        if (i < embed.Length - 1)
                        {
                            tempStr = tempStr.Remove((i - lastCutPosition) + 1);
                        }
                        cutEmbed.Add(tempStr);
                        lastCutPosition = i;
                    }
                }
            }
            return cutEmbed.ToArray();
        }

        public static int GetNextChar(this string s, int startIndex, char targetChar)
        {
            for (int i = startIndex; i < s.Length; i++)
            {
                if (s[i] == targetChar)
                {
                    return i - startIndex;
                }
            }
            return -1;
        }

        public static ArgumentField GetArguments(this string args)
        {
            string[] tempArg = args.Split(' ');
            ArgumentField f = new ArgumentField();

            foreach (string x in tempArg)
            {
                f.arguments.Add(x);
            }

            return f;
        }
    }
}