using Discord;
using IA;
using IA.FileHandling;
using IA.SDK;
using Miki.Accounts;
using Miki.API.Patreon;
using Miki.Languages;
using Miki.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Resources;
using System.Threading.Tasks;

namespace Miki
{
    public class Program
    {
        private static void Main(string[] args)
        {
          new Program().Start().GetAwaiter().GetResult();
        }

        public static Bot bot;

        public static DateTime timeSinceStartup;
        private string devId;

        public async Task Start()
        {
            Locale.Load();
            timeSinceStartup = DateTime.Now;

            LoadApiKeyFromFile();

            LoadDiscord();

            await bot.ConnectAsync();
        }

        private void LoadApiKeyFromFile()
        {
            if (FileReader.FileExist("settings", "miki"))
            {
                FileReader reader = new FileReader("settings", "miki");
                Global.ApiKey = reader.ReadLine();
                devId = reader.ReadLine();
                Global.shardCount = int.Parse(reader.ReadLine());
                Global.CarbonitexKey = reader.ReadLine();
                Global.UrbanKey = reader.ReadLine();
                Global.ImgurKey = reader.ReadLine();
                Global.ImgurClientId = reader.ReadLine();
                Global.DiscordPwKey = reader.ReadLine();
                Global.DiscordBotsOrgKey = reader.ReadLine();
                reader.Finish();
            }
            else
            {
                FileWriter writer = new FileWriter("settings", "miki");
                writer.Write("", "Token");
                writer.Write("", "Developer Id");
                writer.Write("", "Shard Count");
                writer.Write("", "Carbon API Key");
                writer.Write("", "Urban API Key (Mashape)");
                writer.Write("", "Imgur API Key (Mashape)");
                writer.Write("", "Imgur Client ID (without Client-ID)");
                writer.Write("", "Discord.pw API Key");
                writer.Write("", "Discordbot.org API Key");
                writer.Finish();
            }
        }

        /// <summary>
        /// The program runs all discord services and loads all the data here.
        /// </summary>
        public void LoadDiscord()
        {
            bot = new Bot(x =>
            {
                x.Name = "Miki";
                x.Version = "0.3.71";
                x.Token = Global.ApiKey;
                x.ShardCount = Global.shardCount;
                x.ConsoleLogLevel = LogLevel.ALL;
            });

            bot.Events.OnCommandError = async (ex, cmd, msg) =>
            {
                RuntimeEmbed e = new RuntimeEmbed();
                e.Title = Locale.GetEntity(0).GetString(Locale.ErrorMessageGeneric);
                e.Color = new IA.SDK.Color(1, 0.4f, 0.6f);

                if (Notification.CanSendNotification(msg.Author.Id, DatabaseEntityType.USER, DatabaseSettingId.ERRORMESSAGE))
                {
                    e.Description = "Miki has encountered a problem in her code with your request. We will send you a log and instructions through PM.";

                    await msg.Channel.SendMessage(e);

                    e.Title = $"You used the '{cmd.Name}' and it crashed!";
                    e.Description = "Please screenshot this message and send it to the miki issue page (https://github.com/velddev/miki/issues)";
                    e.AddField(f =>
                    {
                        f.Name = "Error Message";
                        f.Value = ex.Message;
                        f.IsInline = true;
                    });

                    e.AddField(f =>
                    {
                        f.Name = "Error Log";
                        f.Value = "```" + ex.StackTrace + "```";
                        f.IsInline = true;
                    });

                    e.CreateFooter();
                    e.Footer.Text = "Did you not want this message? use `>toggleerrors` to disable it!";

                    await msg.Author.SendMessage(e);
                    return;
                }
                e.Description = "... but you've disabled error messages, so we won't send you a PM :)";
                await msg.Channel.SendMessage(e);
            };

            bot.AddDeveloper(121919449996460033);

            if (!string.IsNullOrEmpty(devId))
            {
                bot.AddDeveloper(ulong.Parse(devId));
            }

            bot.Client.JoinedGuild += Client_JoinedGuild;
        }

        private async Task Client_JoinedGuild(IGuild arg)
        {
            ITextChannel defaultChannel = await arg.GetDefaultChannelAsync();
            await defaultChannel.SendMessage("Hello, I am **Miki**! At your service!\nTry to use **>help** to check out what i can do! :notes:");
        }
    }
}