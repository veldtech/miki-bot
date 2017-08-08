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
using NCalc;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
        private string[] lunchposts = new string[]
{
            "https://soundcloud.com/ghostcoffee-342990942/woof-woof-whats-for-lunch?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/lunchpost-1969?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/meian-alien?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/falcon-lunch?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/antique-lunch-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/eternal-bark-engine-shall-we-feast?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/wuff-wuff-whats-for-lunch?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/in-this-woof-monochrome-lunch-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/dogtone?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/ufo-romance-in-the-nut-sky-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/necromastiff?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/the-dumbest-one-on-the-album?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/reach-fur-the-lunch-immurrtal-goat-from-psydo?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/pure-furries-whereabouts-of-the-lunch-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/pawdemic-picnic-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/moon-pup-homunculus-lunch-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/ancient-pups-song-firepsy-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/tummy-rumbling?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/fantasy-nation-lunchbreak-pupper-prayer-1?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/feast-of-the-crysanthemum-canine?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/yin-yang-shiba-serpent?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/yin-yang-shiba-serpent-standalone?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/present-world-oppahaul-lunch-mix?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/food-circulating-melody-native-lunch-owo-remix?in=ghostcoffee-342990942/sets/lunchposting-the-banquet-final-mix",
            "https://soundcloud.com/ghostcoffee-342990942/a-lunch-sample",
            "https://soundcloud.com/ghostcoffee-342990942/something-neato-my-dood",
            "https://soundcloud.com/ghostcoffee-342990942/the-best-one",
            "https://soundcloud.com/ghostcoffee-342990942/pure-gentlemen-whereabouts-of-the-style",
            "https://soundcloud.com/ghostcoffee-342990942/scooby-goo",
            "https://soundcloud.com/ghostcoffee-342990942/lunchstep",
            "https://soundcloud.com/ghostcoffee-342990942/antique-lunch",
            "https://soundcloud.com/ghostcoffee-342990942/take-on-lunch",
            "https://soundcloud.com/ghostcoffee-342990942/bonus-chief-keef-lunchus",
            "https://soundcloud.com/ghostcoffee-342990942/lunch-signal",
            "https://soundcloud.com/ghostcoffee-342990942/silent-woof-2",
            "https://soundcloud.com/ghostcoffee-342990942/wild-lunch",
            "https://soundcloud.com/ghostcoffee-342990942/equivilant-1",
            "https://soundcloud.com/ghostcoffee-342990942/exchange-1",
            "https://soundcloud.com/ghostcoffee-342990942/gangnam-woof-1",
            "https://soundcloud.com/ghostcoffee-342990942/hourai-lunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/lord-of-lunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/lunchvril-14th-1",
            "https://soundcloud.com/ghostcoffee-342990942/making-lunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/megalunchovania",
            "https://soundcloud.com/ghostcoffee-342990942/midnight-lunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/say-whats-for-lunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/silent-woof-1",
            "https://soundcloud.com/ghostcoffee-342990942/stop-lunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/supah-woof-bros-3",
            "https://soundcloud.com/ghostcoffee-342990942/the-worst-one-1",
            "https://soundcloud.com/ghostcoffee-342990942/tnlunch-1",
            "https://soundcloud.com/ghostcoffee-342990942/w-o-o-f-w-a-v-e-1",
            "https://soundcloud.com/ghostcoffee-342990942/whats-for-woof-1",
            "https://soundcloud.com/ghostcoffee-342990942/woofing-in-the-90s-1",
            "https://soundcloud.com/ghostcoffee-342990942/woofline-bling-1"
};

        [Command(Name = "8ball")]
        public async Task EightBallAsync(EventContext e)
        {
            Locale l = Locale.GetEntity(e.Channel.Id.ToDbLong());

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
                "For some reason I like "
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
                " a lot.",
                " a bit.",
                " quite a bit.",
                " a lot, is that weird?",
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
                await e.Channel.SendMessage(Locale.GetEntity(e.Channel.Id.ToDbLong()).GetString(Locale.ImageNotFound));
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
                await e.Channel.SendMessage(Locale.GetEntity(e.Channel.Id.ToDbLong()).GetString(Locale.ImageNotFound));
            }
        }

        [Command(Name = "lunch")]
        public async Task LunchAsync(EventContext e)
        {
            await e.Channel.SendMessage(e.GetResource("lunch_line") + "\n" + lunchposts[Global.random.Next(0, lunchposts.Length)]);
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

            Locale locale = e.Channel.GetLocale();
            await e.Channel.SendMessage(locale.GetString(Locale.PickMessage, new object[] { e.Author.Username, choices[MikiRandom.GetRandomNumber(0, choices.Length)] }));
        }

        [Command(Name = "pun")]
        public async Task PunAsync(EventContext e)
        {
            await e.Channel.SendMessage(Locale.GetEntity(e.Guild.Id.ToDbLong()).GetString(puns[Global.random.Next(0, puns.Length)]));
        }

        [Command(Name = "roll")]
        public async Task RollAsync(EventContext e)
        {
            string output = "";

            if(string.IsNullOrWhiteSpace(e.arguments))
            {
                output = MikiRandom.GetRandomNumber(100).ToString();
            }
            else if (int.TryParse(e.arguments, out int max))
            {
                output = MikiRandom.GetRandomNumber(max).ToString();
            }
            else
            {
                string expression = e.arguments;

                if (!string.IsNullOrWhiteSpace(expression))
                {
                    string[] parts = expression.Split(' ');

                    foreach (string x in parts)
                    {
                        string replacableString = x.Trim('(', ')');
                        int amount = 0;

                        if (replacableString.StartsWith("r"))
                        {
                            string[] split = replacableString.TrimStart('r').Split('d');

                            if (split.Length > 1)
                            {
                                if (int.TryParse(split[0], out int amountOfDice) && int.TryParse(split[1], out int sides))
                                {
                                    int a = 0;
                                    for (int i = 0; i < amountOfDice; i++)
                                    {
                                        a += (MikiRandom.GetRandomNumber(sides) + 1);
                                    }
                                    amount = a;
                                }
                            }
                            else
                            {
                                if (int.TryParse(split[0], out int sides))
                                {
                                    amount = MikiRandom.GetRandomNumber(sides) + 1;
                                }
                            }

                            var regex = new Regex(Regex.Escape(x));

                            expression = regex.Replace(expression, amount.ToString(), 1);
                        }
                    }
                }
                Expression doExpression = new Expression(expression);
                output = doExpression.Evaluate().ToString();
                output += $" ({expression})";
            }

            if (output == "1" || output.StartsWith("1 "))
            {
                await AchievementManager.Instance.GetContainerById("badluck").CheckAsync(new Accounts.Achievements.Objects.BasePacket() { discordUser = e.Author, discordChannel = e.Channel });
            }

            await e.Channel.SendMessage(e.GetResource(Locale.RollResult, e.Author.Username, output));
        }

        [Command(Name = "roulette")]
        public async Task RouletteAsync(EventContext e)
        {
            IEnumerable<IDiscordUser> users = await e.Channel.GetUsersAsync();

            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

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
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

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
                await Utils.ErrorEmbed(locale, "We couldn't find an image with these tags!").SendToChannel(e.Channel);
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

                IDiscordEmbed b = Utils.Embed;
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
                    b.AddField(locale.GetString(Locale.SlotsWinHeader), locale.GetString(Locale.SlotsWinMessage, moneyReturned));
                }

                b.Description = string.Join(" ", objectsChosen);
                u.Currency += moneyReturned;
                await context.SaveChangesAsync();
                await b.SendToChannel(e.Channel);
            }
        }
    }
}
