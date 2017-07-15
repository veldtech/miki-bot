using Discord;
using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.Extension;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using IMDBNet;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;
using Miki.Accounts;
using Miki.Accounts.Achievements;
using Miki.API;
using Miki.Languages;
using Miki.Models;
using Miki.Objects;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module(Name = "Fun")]
    public class FunModule
    {
        string[] puns = new string[]
        {
                "miki_module_fun_pun_1",
                "miki_module_fun_pun_2",
                "miki_module_fun_pun_3",
                "miki_module_fun_pun_4",
                "miki_module_fun_pun_5",
                "miki_module_fun_pun_6",
                "miki_module_fun_pun_7",
                "miki_module_fun_pun_8",
                "miki_module_fun_pun_9",
                "miki_module_fun_pun_10",
                "miki_module_fun_pun_11",
                "miki_module_fun_pun_12",
                "miki_module_fun_pun_13",
                "miki_module_fun_pun_14",
                "miki_module_fun_pun_15",
                "miki_module_fun_pun_16",
                "miki_module_fun_pun_17",
                "miki_module_fun_pun_18",
                "miki_module_fun_pun_19",
                "miki_module_fun_pun_20",
                "miki_module_fun_pun_21",
                "miki_module_fun_pun_22",
                "miki_module_fun_pun_23",
                "miki_module_fun_pun_24",
                "miki_module_fun_pun_25",
                "miki_module_fun_pun_26",
                "miki_module_fun_pun_27",
                "miki_module_fun_pun_28",
                "miki_module_fun_pun_29",
                "miki_module_fun_pun_30",
                "miki_module_fun_pun_31",
                "miki_module_fun_pun_32",
                "miki_module_fun_pun_33",
                "miki_module_fun_pun_34",
                "miki_module_fun_pun_35",
                "miki_module_fun_pun_36",
                "miki_module_fun_pun_37",
                "miki_module_fun_pun_38",
                "miki_module_fun_pun_39",
                "miki_module_fun_pun_40",
                "miki_module_fun_pun_41",
                "miki_module_fun_pun_42",
                "miki_module_fun_pun_43",
                "miki_module_fun_pun_44",
                "miki_module_fun_pun_45",
                "miki_module_fun_pun_46",
                "miki_module_fun_pun_47",
                "miki_module_fun_pun_48",
                "miki_module_fun_pun_49",
                "miki_module_fun_pun_50",
                "miki_module_fun_pun_51",
                "miki_module_fun_pun_52",
        };
        string[] reactions = new string[]
        {
                "miki_module_fun_8ball_answer_negative_1",
                "miki_module_fun_8ball_answer_negative_2",
                "miki_module_fun_8ball_answer_negative_3",
                "miki_module_fun_8ball_answer_negative_4",
                "miki_module_fun_8ball_answer_negative_5",
                "miki_module_fun_8ball_answer_neutral_1",
                "miki_module_fun_8ball_answer_neutral_2",
                "miki_module_fun_8ball_answer_neutral_3",
                "miki_module_fun_8ball_answer_neutral_4",
                "miki_module_fun_8ball_answer_neutral_5",
                "miki_module_fun_8ball_answer_positive_1",
                "miki_module_fun_8ball_answer_positive_2",
                "miki_module_fun_8ball_answer_positive_3",
                "miki_module_fun_8ball_answer_positive_4",
                "miki_module_fun_8ball_answer_positive_5",
                "miki_module_fun_8ball_answer_positive_6",
                "miki_module_fun_8ball_answer_positive_7",
                "miki_module_fun_8ball_answer_positive_8",
                "miki_module_fun_8ball_answer_positive_9"
        };

        [Command(Name = "8ball")]
        public async Task EightBallAsync(EventContext e)
        {
            Locale l = Locale.GetEntity(e.Guild.Id.ToDbLong());

            string output = l.GetString("miki_module_fun_8ball_result", new object[] { e.Author.Username, l.GetString(reactions[Global.random.Next(0, reactions.Length)]) });
            await e.Channel.SendMessage(output);
        }

        [Command(Name = "bird")]
        public async Task BirdAsync(EventContext e)
        {
            string[] bird = new string[]
            {
                "http://i.imgur.com/aN948tq.jpg",
                "http://i.imgur.com/cYPsbR5.jpg",
                "http://i.imgur.com/18sRay4.jpg",
                "http://i.imgur.com/8wQYgvb.jpg",
                "http://i.imgur.com/aH32RFJ.jpg",
                "http://i.imgur.com/xP9PDiZ.jpg",
                "http://i.imgur.com/pxUOTw3.jpg",
                "http://i.imgur.com/CsDvncx.jpg",
                "http://i.imgur.com/Z5Gy32e.jpg",
                "http://i.imgur.com/4lUEQJ1.jpg",
                "http://i.imgur.com/GMkfhRN.jpg",
                "http://i.imgur.com/wTo8jAZ.jpg",
                "http://i.imgur.com/ztY0Jfs.jpg",
                "http://i.imgur.com/F7xwjRj.jpg",
                "http://i.imgur.com/N5Sn8DJ.jpg",
                "http://i.imgur.com/LNL0nuX.jpg",
                "http://i.imgur.com/DT6OVOL.jpg",
                "http://i.imgur.com/9RoC12z.jpg",
                "http://i.imgur.com/eK81Cyt.jpg",
                "http://i.imgur.com/YTz2P1p.jpg",
                "http://i.imgur.com/2nukcqZ.jpg",
                "http://i.imgur.com/BxwgwHh.jpg"
            };
            await e.Channel.SendMessage(bird[Global.random.Next(0, bird.Length)]);
        }

        [Command(Name = "cat")]
        public async Task CatAsync(EventContext e)
        {
            WebClient c = new WebClient();
            byte[] b = c.DownloadData("http://random.cat/meow");
            string str = Encoding.Default.GetString(b);
            CatImage cat = JsonConvert.DeserializeObject<CatImage>(str);
            await e.Channel.SendMessage(cat.File);
        }

        [Command(Name = "compliment")]
        public async Task ComplimentAsync(EventContext e)
        {
            string[] I_LIKE = new string[]
            {
                "I like ",
                "I love ",
                "I admire ",
                "I really enjoy ",
                "For some reason i like "
            };

            string[] BODY_PART = new string[]
            {
                "that silly fringe of yours",
                "the lower part of your lips",
                "the smallest toe on your left foot",
                "the smallest toe on your right foot",
                "the second eyelash from your left eye",
                "the lower part of your chin",
                "your creepy finger in your left hand",
                "your cute smile",
                "those dazzling eyes of yours",
                "your creepy finger in your right hand",
                "the special angles your elbows makes",
                "the dimples on your cheeks",
                "your smooth hair"
            };

            string[] SUFFIX = new string[]
            {
                " alot",
                " a bit",
                " quite a bit",
                " a lot, is that weird?",
                ""
            };

            await e.Channel.SendMessage(I_LIKE[Global.random.Next(0, I_LIKE.Length)] + BODY_PART[Global.random.Next(0, BODY_PART.Length)] + SUFFIX[Global.random.Next(0, SUFFIX.Length)]);

        }

        [Command(Name = "cage")]
        public async Task CageAsync(EventContext e)
        {
            await e.Channel.SendMessage("http://www.placecage.com/c/" + Global.random.Next(100, 1500) + "/" + Global.random.Next(100, 1500));
        }

        [Command(Name = "ctb")]
        public async Task SendCatchTheBeatSignatureAsync(EventContext e)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] data = webClient.DownloadData("http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + e.arguments + "&mode=2&countryrank");


                using (MemoryStream mem = new MemoryStream(data))
                {
                    await e.Channel.SendFileAsync(mem, $"{e.arguments}.png");
                }
            }
        }

        [Command(Name = "dog")]
        public async Task DogAsync(EventContext e)
        {
            string[] dog = new string[]
            {
                "http://i.imgur.com/KOjUbMQ.jpg",
                "http://i.imgur.com/owJKr7y.jpg",
                "http://i.imgur.com/rpQdRoY.jpg",
                "http://i.imgur.com/xrVXDQ2.jpg",
                "http://i.imgur.com/Bt6zrhq.jpg",
                "http://i.imgur.com/sNm8hRR.jpg",
                "http://i.imgur.com/TeTwhxN.jpg",
                "http://i.imgur.com/zMs8Sx6.jpg",
                "http://i.imgur.com/bWioilW.jpg",
                "http://i.imgur.com/q6UTI0W.jpg",
                "http://i.imgur.com/e9aTxh7.jpg",
                "http://i.imgur.com/lvH26a9.jpg",
                "http://i.imgur.com/0Q8E82b.jpg",
                "http://i.imgur.com/oMm6Zba.jpg",
                "http://i.imgur.com/aA4kvJE.jpg",
                "http://i.imgur.com/FJRTLZR.jpg",
                "http://i.imgur.com/EHYhgJk.jpg",
                "http://i.imgur.com/QiudnCT.jpg",
                "http://i.imgur.com/2nYIwTd.jpg",
                "http://i.imgur.com/NnFZVPC.jpg",
                "http://i.imgur.com/uEHtSpB.jpg",
                "http://i.imgur.com/5DPqIi0.jpg",
                "http://i.imgur.com/ZkxnaRE.jpg",
                "http://i.imgur.com/kCWMUgk.jpg",
                "http://i.imgur.com/X8FO7Ds.jpg",
                "http://i.imgur.com/yKAXiyl.jpg",
                "http://i.imgur.com/A4eVQQF.jpg",
                "http://i.imgur.com/Wtjxxiv.jpg",
                "http://i.imgur.com/uIX5RVf.jpg",
                "http://i.imgur.com/49Da81l.jpg"
            };
            await e.Channel.SendMessage(dog[Global.random.Next(0, dog.Length)]);
        }

        [Command(Name = "gif")]
        public async Task ImgurGifAsync(EventContext e)
        {
            if (string.IsNullOrEmpty(e.arguments)) return;

            var client = new MashapeClient(Global.ImgurClientId, Global.ImgurKey);
            var endpoint = new GalleryEndpoint(client);
            var images = await endpoint.SearchGalleryAsync($"title:{e.arguments} ext:gif");
            List<IGalleryImage> actualImages = new List<IGalleryImage>();
            foreach (IGalleryItem item in images)
            {
                if (item as IGalleryImage != null)
                {
                    actualImages.Add(item as IGalleryImage);
                }
            }

            if (actualImages.Count > 0)
            {
                IGalleryImage i = actualImages[Global.random.Next(0, actualImages.Count)];

                await e.Channel.SendMessage(i.Link);
            }
            else
            {
                await e.Channel.SendMessage(Locale.GetEntity(e.Guild.Id.ToDbLong()).GetString(Locale.ImageNotFound));
            }
        }

        [Command(Name = "img")]
        public async Task ImgurImageAsync(EventContext e)
        {
            if (string.IsNullOrEmpty(e.arguments)) return;

            var client = new MashapeClient(Global.ImgurClientId, Global.ImgurKey);
            var endpoint = new GalleryEndpoint(client);
            var images = await endpoint.SearchGalleryAsync($"title:{e.arguments}");
            List<IGalleryImage> actualImages = new List<IGalleryImage>();
            foreach (IGalleryItem item in images)
            {
                if (item as IGalleryImage != null)
                {
                    actualImages.Add(item as IGalleryImage);
                }
            }

            if (actualImages.Count > 0)
            {
                IGalleryImage i = actualImages[Global.random.Next(0, actualImages.Count)];

                await e.Channel.SendMessage(i.Link);
            }
            else
            {
                await e.Channel.SendMessage(Locale.GetEntity(e.Guild.Id.ToDbLong()).GetString(Locale.ImageNotFound));
            }
        }

        [Command(Name = "mania")]
        public async Task SendManiaSignatureAsync(EventContext e)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] data = webClient.DownloadData("http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + e.arguments + "&mode=3&countryrank");

                using (MemoryStream mem = new MemoryStream(data))
                {
                    await e.Channel.SendFileAsync(mem, $"sig.png");
                }
            }
        }

        [Command(Name = "osu")]
        public async Task SendOsuSignatureAsync (EventContext e)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] data = webClient.DownloadData("http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + e.arguments + "&countryrank");

                using (MemoryStream mem = new MemoryStream(data))
                {
                    await e.Channel.SendFileAsync(mem, $"sig.png");
                }
            }
        }

        [Command(Name = "pick")]
        public async Task PickAsync(EventContext e)
        {
            if (string.IsNullOrWhiteSpace(e.arguments))
            {
                await e.Channel.SendMessage(Locale.GetEntity(e.Guild.Id.ToDbLong()).GetString(Locale.ErrorPickNoArgs));
                return;
            }
            string[] choices = e.arguments.Split(',');

            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());
            await e.Channel.SendMessage(locale.GetString(Locale.PickMessage, new object[] { e.Author.Username, choices[Global.random.Next(0, choices.Length)] }));
        }

        [Command(Name = "pun")]
        public async Task PunAsync(EventContext e)
        {
            await e.Channel.SendMessage(Locale.GetEntity(e.Guild.Id.ToDbLong()).GetString(puns[Global.random.Next(0, puns.Length)]));
        }

        [Command(Name = "roll")]
        public async Task RollAsync(EventContext e)
        {
            string rollCalc = "";
            string amount = "";
            int rollAmount = 0;

            if (e.arguments != "")
            {
                amount = e.arguments.Split(' ')[0];

                if (amount.Split('d').Length > 1)
                {
                    for (int i = 0; i < int.Parse(amount.Split('d')[0]); i++)
                    {
                        int num = Mathm.Roll(int.Parse(amount.Split('d')[1]), 0);
                        rollAmount += num;
                        rollCalc += num + " + ";
                    }
                    rollCalc = rollCalc.Remove(rollCalc.Length - 3);
                }
                else
                {
                    try
                    {
                        rollAmount = Mathm.Roll(int.Parse(amount), 0);
                    }
                    catch
                    {
                        rollAmount = Mathm.Roll();

                    }
                }
            }
            else
            {
                rollAmount = Mathm.Roll();
            }

            if (rollAmount == 1)
            {
                await AchievementManager.Instance.GetContainerById("badluck").CheckAsync(new Accounts.Achievements.Objects.BasePacket() { discordUser = e.Author, discordChannel = e.Channel });
            }


            await e.Channel.SendMessage(Locale.GetEntity(e.Guild.Id.ToDbLong()).GetString(Locale.RollResult, new object[] { e.Author.Username, rollAmount }) + (rollCalc != "" ? " (" + rollCalc + ")" : ""));
        }

        [Command(Name = "roulette")]
        public async Task RouletteAsync(EventContext e)
        {
            IEnumerable<IDiscordUser> users = await e.Channel.GetUsersAsync();

            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            if (e.message.Content.Split(' ').Length == 1)
            {
                await e.Channel.SendMessage(locale.GetString(Locale.RouletteMessageNoArg, new object[] { "<@" + users.ElementAt(Global.random.Next(0, users.Count())).Id + ">" }));
            }
            else
            {
                await e.Channel.SendMessage(locale.GetString(Locale.RouletteMessage, new object[] { e.arguments, "<@" + users.ElementAt(Global.random.Next(0, users.Count())).Id + ">" }));
            }
        }

        [Command(Name = "taiko")]
        public async Task SendTaikoSignatureAsync(EventContext e)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] data = webClient.DownloadData("http://lemmmy.pw/osusig/sig.php?colour=pink&uname=" + e.arguments + "&mode=1&countryrank");

                using (MemoryStream mem = new MemoryStream(data))
                {
                    await e.Channel.SendFileAsync(mem, $"sig.png");
                }
            }
        }

        [Command(Name = "slots")]
        public async Task SlotsAsync(EventContext e)
        {
            int moneyBet = 0;

            using (var context = new MikiContext())
            {
                User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

                if (!string.IsNullOrWhiteSpace(e.arguments))
                {
                    moneyBet = int.Parse(e.arguments);

                    if (moneyBet > u.Currency)
                    {
                        await e.Channel.SendMessage(locale.GetString(Locale.InsufficientMekos));
                        return;
                    }
                }

                int moneyReturned = 0;

                if (moneyBet <= 0)
                {
                    return;
                }

                string[] objects =
                {
                    "🍒", "🍒", "🍒", "🍒",
                    "🍊", "🍊",
                    "🍓", "🍓",
                    "🍍","🍍",
                    "🍇", "🍇",
                    "⭐", "⭐",
                    "🍍", "🍍",
                    "🍓", "🍓",
                    "🍊", "🍊", "🍊",
                    "🍒", "🍒", "🍒", "🍒",
                };

                EmbedBuilder b = new EmbedBuilder();
                b.Title = locale.GetString(Locale.SlotsHeader);

                Random r = new Random();

                string[] objectsChosen =
                {
                    objects[r.Next(objects.Length)],
                    objects[r.Next(objects.Length)],
                    objects[r.Next(objects.Length)]
                };

                Dictionary<string, int> score = new Dictionary<string, int>();

                foreach (string o in objectsChosen)
                {
                    if (score.ContainsKey(o))
                    {
                        score[o]++;
                        continue;
                    }
                    score.Add(o, 1);
                }

                if (score.ContainsKey("🍒"))
                {
                    if (score["🍒"] == 2)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 0.5f);
                    }
                    else if (score["🍒"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 1f);
                    }
                }
                if (score.ContainsKey("🍊"))
                {
                    if (score["🍊"] == 2)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 0.8f);
                    }
                    else if (score["🍊"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 1.5f);
                    }
                }
                if (score.ContainsKey("🍓"))
                {
                    if (score["🍓"] == 2)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 1f);
                    }
                    else if (score["🍓"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 2f);
                    }
                }
                if (score.ContainsKey("🍍"))
                {
                    if (score["🍍"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 4f);
                    }
                }
                if (score.ContainsKey("🍇"))
                {
                    if (score["🍇"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 6f);
                    }
                }
                if (score.ContainsKey("⭐"))
                {
                    if (score["⭐"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 12f);
                    }
                }

                if (moneyReturned == 0)
                {
                    moneyReturned = -moneyBet;
                }
                else
                {
                    b.AddField(f =>
                    {
                        f.Name = locale.GetString(Locale.SlotsWinHeader);
                        f.Value = locale.GetString(Locale.SlotsWinMessage, moneyReturned);
                    });
                }

                b.Description = string.Join(" ", objectsChosen);
                u.Currency += moneyReturned;
                await context.SaveChangesAsync();
                await e.Channel.SendMessage(new RuntimeEmbed(b));
            }
        }

        //[Command(Name = "slots", On = "all")]
        //public async Task SlotsAllAsync(EventContext e)
        //{
        //    Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

        //    using (var context = new MikiContext())
        //    {
        //        User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());
        //        await InternalSlotsAsync(e, e.Author.Id, locale, u.Currency);
        //    }
        //}

        [Command(Name = "remind")]
        public async Task DoRemind(EventContext e)
        {
            List<string> arguments = e.arguments.Split(' ').ToList();
            int splitIndex = 0;

            for(int i = 0; i < arguments.Count; i++)
            {
                if(arguments[i].ToLower() == "in")
                {
                    splitIndex = i;
                }
            }

            if(splitIndex == 0)
            {
                // throw error
                return;
            }

            string reminderText;

            int count = arguments.Count;
            arguments.RemoveRange(splitIndex, count - (splitIndex));
            reminderText = string.Join(" ", arguments);

            if (reminderText.StartsWith("me to "))
            {
                reminderText = reminderText.Substring(6);
            }

            TimeSpan timeUntilReminder = e.arguments.GetTimeFromString();

            await Utils.Embed
                .SetTitle("👌 OK")
                .SetDescription($"I'll remind you to **{reminderText}** in **{timeUntilReminder.ToTimeString()}**")
                .SetColor(IA.SDK.Color.GetColor(IAColor.GREEN))
                .SendToChannel(e.Channel.Id);

            await new ReminderAPI(e.Author.Id)
                .Remind(reminderText, timeUntilReminder)
                .Listen();
        }

        [Command(Name = "safe")]
        public async Task DoSafe(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Guild.Id.ToDbLong());

            IPost s = null;
            if (e.arguments.ToLower().StartsWith("use"))
            {
                string[] a = e.arguments.Split(' ');
                e.arguments = e.arguments.Substring(a[0].Length);
                switch (a[0].Split(':')[1].ToLower())
                {
                    case "safebooru":
                        {
                            s = SafebooruPost.Create(e.arguments, ImageRating.SAFE);
                        }
                        break;
                    case "gelbooru":
                        {
                            s = GelbooruPost.Create(e.arguments, ImageRating.SAFE);
                        }
                        break;
                    case "konachan":
                        {
                            s = KonachanPost.Create(e.arguments, ImageRating.SAFE);
                        }
                        break;
                    case "e621":
                        {
                            s = E621Post.Create(e.arguments, ImageRating.SAFE);
                        }
                        break;
                    default:
                        {
                            await e.Channel.SendMessage("I do not support this image host :(");
                        }
                        break;
                }
            }
            else
            {
                s = SafebooruPost.Create(e.arguments, ImageRating.SAFE);
            }

            if (s == null)
            {
                await e.Channel.SendMessage(Utils.ErrorEmbed(locale, "We couldn't find an image with these tags!"));
                return;
            }

            await e.Channel.SendMessage(s.ImageUrl);
        }

        private async Task InternalSlotsAsync(EventContext e, ulong userid, Locale locale, int amount)
        {
            int moneyBet = 0;

            using (var context = new MikiContext())
            {
                User u = await context.Users.FindAsync(userid.ToDbLong());

                int moneyReturned = 0;

                if (moneyBet <= 0)
                {
                    return;
                }

                string[] objects =
                {
                    "🍒", "🍒", "🍒", "🍒",
                    "🍊", "🍊",
                    "🍓", "🍓",
                    "🍍","🍍",
                    "🍇", "🍇",
                    "⭐", "⭐",
                    "🍍", "🍍",
                    "🍓", "🍓",
                    "🍊", "🍊", "🍊",
                    "🍒", "🍒", "🍒", "🍒",
                };

                EmbedBuilder b = new EmbedBuilder();
                b.Title = locale.GetString(Locale.SlotsHeader);

                Random r = new Random();

                string[] objectsChosen =
                {
                    objects[r.Next(objects.Length)],
                    objects[r.Next(objects.Length)],
                    objects[r.Next(objects.Length)]
                };

                Dictionary<string, int> score = new Dictionary<string, int>();

                foreach (string o in objectsChosen)
                {
                    if (score.ContainsKey(o))
                    {
                        score[o]++;
                        continue;
                    }
                    score.Add(o, 1);
                }

                if (score.ContainsKey("🍒"))
                {
                    if (score["🍒"] == 2)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 0.5f);
                    }
                    else if (score["🍒"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 1f);
                    }
                }
                if (score.ContainsKey("🍊"))
                {
                    if (score["🍊"] == 2)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 0.8f);
                    }
                    else if (score["🍊"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 1.5f);
                    }
                }
                if (score.ContainsKey("🍓"))
                {
                    if (score["🍓"] == 2)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 1f);
                    }
                    else if (score["🍓"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 2f);
                    }
                }
                if (score.ContainsKey("🍍"))
                {
                    if (score["🍍"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 4f);
                    }
                }
                if (score.ContainsKey("🍇"))
                {
                    if (score["🍇"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 6f);
                    }
                }
                if (score.ContainsKey("⭐"))
                {
                    if (score["⭐"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 12f);
                    }
                }

                if (moneyReturned == 0)
                {
                    moneyReturned = -moneyBet;
                }
                else
                {
                    b.AddField(f =>
                    {
                        f.Name = locale.GetString(Locale.SlotsWinHeader);
                        f.Value = locale.GetString(Locale.SlotsWinMessage, moneyReturned);
                    });
                }

                b.Description = string.Join(" ", objectsChosen);
                u.Currency += moneyReturned;
                await context.SaveChangesAsync();
                await e.Channel.SendMessage(new RuntimeEmbed(b));
            }
        }

    }
}